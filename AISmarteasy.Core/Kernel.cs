using AISmarteasy.Core.Config;
using AISmarteasy.Core.Connector;
using AISmarteasy.Core.Connector.OpenAI;
using AISmarteasy.Core.Connector.OpenAI.Text;
using AISmarteasy.Core.Connector.OpenAI.Text.Chat;
using AISmarteasy.Core.Context;
using AISmarteasy.Core.Function;
using AISmarteasy.Core.Handler;
using AISmarteasy.Core.Memory;
using AISmarteasy.Core.Planner;
using AISmarteasy.Core.Prompt;
using AISmarteasy.Core.Service;
using Microsoft.Extensions.Logging;
using Directory = System.IO.Directory;
using File = System.IO.File;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace AISmarteasy.Core;

public sealed class Kernel// : IDisposable
{
    private const string SEMANTIC_PLUGIN_CONFIG_FILE = "config.json";
    private const string SEMANTIC_PLUGIN_PROMPT_FILE = "skprompt.txt";
    private readonly string _semanticPluginDirectory;
    private readonly ILogger _logger;
    private ISemanticMemory? _memory;

    public Dictionary<string, Plugin> Plugins { get; }
    public IPromptTemplate PromptTemplate { get; }
    public PromptTemplateConfig PromptTemplateConfig { get; }
    public ILoggerFactory LoggerFactory { get; }
    public IDelegatingHandlerFactory HttpHandlerFactory { get; }
    public ITextCompletion TextCompletionService { get; }
    public IEmbeddingGeneration? EmbeddingService { get; private set; }
    public IImageGeneration? ImageGenerationService { get; set; }
    public SKContext Context { get; set; }

    public Kernel(ITextCompletion textCompletionService, IDelegatingHandlerFactory httpHandlerFactory, ILoggerFactory loggerFactory)
    {
        TextCompletionService = textCompletionService;
        HttpHandlerFactory = httpHandlerFactory;
        LoggerFactory = loggerFactory;

        Context = new SKContext(loggerFactory: loggerFactory);
        _logger = LoggerFactory.CreateLogger(typeof(Kernel));

        PromptTemplate = new PromptTemplate(LoggerFactory);
        PromptTemplateConfig = PromptTemplateConfigBuilder.Build();

        Plugins = new Dictionary<string, Plugin>();

        _semanticPluginDirectory = Path.Combine(Directory.GetCurrentDirectory(), "plugins", "semantic"); 
        LoadSemanticPlugin(); 
    }

    public string ContextVariablesInput => KernelProvider.Kernel.Context.Variables.Input;
    public string Result => Context.Result;

    public Task RunFunctionAsync(FunctionRunConfig config)
    {
        var function = FindFunction(config.PluginName, config.FunctionName);
        return RunFunctionAsync(function, config.Parameters);
    }

    public Task RunFunctionAsync(ISKFunction function, string prompt)
    {
        var config = new FunctionRunConfig();
        config.UpdateInput(prompt);
        return RunFunctionAsync(function, config.Parameters);
    }

    public Task RunFunctionAsync(ISKFunction function, Dictionary<string, string> parameters)
    {
        foreach (var parameter in parameters)
        {
            Context.Variables[parameter.Key] = parameter.Value;
        }

        return function.InvokeAsync(function.RequestSettings);
    }

    public Task RunFunctionAsync(string prompt)
    {
        var config = new FunctionRunConfig();
        config.UpdateInput(prompt);
        var function = FindFunction("QASkill", "Question");
        return RunFunctionAsync(function, config.Parameters);
    }

    public async Task<bool> SaveMemoryFromPdfDirectory(string directory)
    {
        if (_memory != null) 
            return await Embedding.SaveFromPdfDirectory(_memory, directory).ConfigureAwait(false);
        return false;
    }

    public async Task<bool> SaveMemoryAsync(Dictionary<string, string> textData)
    {
        if (_memory != null)
            return await Embedding.SaveAsync(_memory, textData).ConfigureAwait(false);
        return false;
    }

    public async Task<IAsyncEnumerable<MemoryQueryResult>?> SearchMemoryAsync(string query)
    {
        if (_memory == null)
        {
            return null;
        }
        
        return await Embedding.SearchAsync(_memory, query).ConfigureAwait(false);
    }

    public void UseMemory(IEmbeddingGeneration embeddingService, IMemoryStore storage)
    {
        Verify.NotNull(storage);
        Verify.NotNull(embeddingService);
        RegisterMemory(embeddingService, storage);
    }

    public async Task<Plan> RunPlanAsync(string goal)
    {
        Plan? plan;
        try
        {
            var planBuilder = new PlanBuilder();
            plan = await planBuilder.Build(goal).ConfigureAwait(false);

            while (plan.HasNextStep)
            {
                var requestSetting = AIRequestSettings.FromCompletionConfig(PromptTemplateConfig.Completion);
                await plan.RunAsync(requestSetting).ConfigureAwait(false);
            }
        }
        catch (SKException e)
        {
            Console.WriteLine(e);
            throw;
        }

        return plan;
    }

    public string BuildFunctionViews()
    {
        var result = string.Empty;
        foreach (var plugin in Plugins.Values)
        {
            result += string.Join("\n\n", plugin.BuildPluginView().FunctionViews.Values.Select(x => x.ToManualString()));
            result += "\n\n";
        }

        return result;
    }

    public async Task<ChatHistory> StartChatCompletionAsync(string systemMessage)
    {
        var chatHistory = TextCompletionService.CreateNewChat(systemMessage);
        var requestSetting = AIRequestSettings.FromCompletionConfig(PromptTemplateConfig.Completion);
        return await TextCompletionService.RunChatCompletionAsync(chatHistory, requestSetting).ConfigureAwait(false);
    }

    public Task<ChatHistory> RunChatCompletionAsync(ChatHistory history)
    {
        var requestSetting = AIRequestSettings.FromCompletionConfig(PromptTemplateConfig.Completion);
        return TextCompletionService.RunChatCompletionAsync(history, requestSetting);
    }

    public Task<SemanticAnswer> RunTextCompletionAsync(string prompt)
    {
        var requestSetting = AIRequestSettings.FromCompletionConfig(PromptTemplateConfig.Completion);
        return TextCompletionService.RunTextCompletionAsync(prompt, requestSetting);
    }

    public IAsyncEnumerable<TextStreamingResult> RunTextStreamingCompletionAsync(string prompt)
    {
        var requestSetting = AIRequestSettings.FromCompletionConfig(PromptTemplateConfig.Completion);
        return TextCompletionService.RunTextStreamingCompletionAsync(prompt, requestSetting);
    }

    public IAsyncEnumerable<IChatStreamingResult> RunChatStreamingAsync(ChatHistory chatHistory)
    {
        var requestSetting = AIRequestSettings.FromCompletionConfig(PromptTemplateConfig.Completion);
        return TextCompletionService.RunChatStreamingCompletionAsync(chatHistory, requestSetting);
    }

    public ISKFunction FindFunction(string pluginName, string functionName)
    {
        Verify.ValidPluginName(pluginName);
        Verify.ValidFunctionName(functionName);

        Plugins.TryGetValue(pluginName, out var plugin);
        return plugin!.GetFunction(functionName);
    }

    public async Task<SemanticAnswer> RunPipelineAsync(PipelineRunConfig config)
    {
        int pipelineStepCount = 0;

        foreach (var pluginFunctionName in config.PluginFunctionNames)
        {
            try
            {
                var function = FindFunction(pluginFunctionName.PluginName, pluginFunctionName.FunctionName);
                await RunFunctionAsync(function, config.Parameters).ConfigureAwait(false);
                config.Parameters.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError("Plugin {Plugin} function {Function} call fail during pipeline step {Step} with error {Error}:", 
                    pluginFunctionName.PluginName, pluginFunctionName.FunctionName, pipelineStepCount, ex.Message);
                throw;
            }

            pipelineStepCount++;
        }

        return new SemanticAnswer(ContextVariablesInput);
    }

    private ISKFunction CreateSemanticFunction(SemanticFunctionConfig config)
    {
        if (!config.PromptTemplateConfig.Type.Equals("completion", StringComparison.OrdinalIgnoreCase))
        {
            throw new SKException($"Function type not supported: {config.PromptTemplateConfig}");
        }

        var func = SemanticFunction.FromSemanticConfig(config.PluginName, config.FunctionName, config, LoggerFactory);

        func.SetAIConfiguration(AIRequestSettings.FromCompletionConfig(config.PromptTemplateConfig.Completion));

        return func;
    }



    private void LoadSemanticPlugin()
    {
        string[] subDirectories = Directory.GetDirectories(_semanticPluginDirectory);
        LoadSemanticPlugin(subDirectories);
    }

    private void LoadSemanticPlugin(string[] subDirectories)
    {
        foreach (var directory in subDirectories)
        {
            LoadSemanticSubPlugin(directory, Directory.GetDirectories(directory));
        }
        LoadSemanticSubPlugin(_semanticPluginDirectory, subDirectories);
    }

    private void LoadSemanticSubPlugin(string parentDirectoryName, string[] subDirectories)
    {
        foreach (var subDirectory in subDirectories)
        {
            var promptPath = Path.Combine(subDirectory, SEMANTIC_PLUGIN_PROMPT_FILE);
            if (File.Exists(promptPath))
            {
                var pluginName = Path.GetFileName(parentDirectoryName);
                var functionName = Path.GetFileName(subDirectory);
                LoadSemanticFunction(pluginName, functionName, subDirectory);
            }
            else
            {
                LoadSemanticSubPlugin(subDirectory, Directory.GetDirectories(subDirectory));
            }
        }
    }

    private void LoadSemanticFunction(string pluginName, string functionName, string directoryPath)
    {
        var promptPath = Path.Combine(directoryPath, SEMANTIC_PLUGIN_PROMPT_FILE);
        var configPath = Path.Combine(directoryPath, SEMANTIC_PLUGIN_CONFIG_FILE);

        var config = PromptTemplateConfig.FromJson(File.ReadAllText(configPath));
        var template = new PromptTemplate(File.ReadAllText(promptPath), config);
        var functionConfig = new SemanticFunctionConfig(pluginName, functionName, config, template);

        RegisterSemanticFunction(functionConfig);

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace($"Config {functionName}: {config.ToJson()}");
            _logger.LogTrace($"Registering function {pluginName}.{functionName}");
        }
    }

    public ISKFunction RegisterSemanticFunction(SemanticFunctionConfig config)
    {
        var pluginName = config.PluginName;
        Verify.ValidPluginName(pluginName);
        Verify.ValidFunctionName(config.FunctionName);

        ISKFunction function = CreateSemanticFunction(config);

        if (!Plugins.TryGetValue(pluginName, out var plugin))
        {
            plugin = new Plugin(pluginName);
            Plugins.Add(pluginName, plugin);
        }

        plugin.AddFunction(function);

        return function;
    }

    public void RegisterNativeFunction(ISKFunction function)
    {
        var pluginName = function.PluginName;
        if (!Plugins.TryGetValue(pluginName, out var plugin))
        {
            plugin = new Plugin(pluginName);
            Plugins.Add(pluginName, plugin);
        }

        plugin.AddFunction(function);
    }
    
    public void RegisterMemory(IEmbeddingGeneration embeddingService, IMemoryStore storage)
    {
        EmbeddingService = embeddingService;
        _memory = new SemanticMemory(embeddingService, storage);
    }

    public SKContext CreateNewContext(ContextVariables variables)
    {
        return new SKContext(variables);
    }

    public async Task<string?> GenerateImageAsync(string description, int width, int height)
    {
        if (ImageGenerationService != null)
        {
            return await ImageGenerationService.GenerateImageAsync(description, width, height).ConfigureAwait(false);
        }

        return null;
    }
}

//public void Dispose()
//    {
//        if (_memory is IDisposable mem) { mem.Dispose(); }
//        if (Plugins is IDisposable plugins) { plugins.Dispose(); }
//    }


