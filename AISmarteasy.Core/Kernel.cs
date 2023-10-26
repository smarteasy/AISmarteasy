using AISmarteasy.Core.Connecting;
using AISmarteasy.Core.Connecting.OpenAI;
using AISmarteasy.Core.Connecting.OpenAI.Text;
using AISmarteasy.Core.Connecting.OpenAI.Text.Chat;
using AISmarteasy.Core.Context;
using AISmarteasy.Core.PluginFunction;
using AISmarteasy.Core.Handling;
using AISmarteasy.Core.Memory;
using AISmarteasy.Core.Planning;
using AISmarteasy.Core.Prompt;
using AISmarteasy.Core.Service;
using Microsoft.Extensions.Logging;
using Directory = System.IO.Directory;
using File = System.IO.File;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace AISmarteasy.Core;

public sealed class Kernel
{
    private const string SEMANTIC_PLUGIN_CONFIG_FILE = "config.json";
    private const string SEMANTIC_PLUGIN_PROMPT_FILE = "skprompt.txt";
    private readonly string _semanticPluginDirectory;
    private readonly ILogger _logger;
    private ISemanticMemory? _memory;

    private List<string> _includedFunctionViews = new List<string>();

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
    }

    public void LoadPlugin()
    {
        LoadSemanticPlugin();
        LoadNativePlugin();
    }

    private void LoadNativePlugin()
    {
        foreach (var function in NativeFunctionLoader.Load().Values)
        {
            RegisterNativeFunction(function);
        }

        NativeFunctionLoader.Load();
    }

    public string ContextVariablesInput => Context.Variables.Input;
    public string Result => Context.Result;

    public Task RunFunctionAsync(FunctionRunConfig config)
    {
        var function = FindFunction(config.PluginName, config.FunctionName);
        return RunFunctionAsync(function, config.Parameters);
    }

    public Task RunFunctionAsync(Function function, string prompt)
    {
        var config = new FunctionRunConfig();
        config.UpdateInput(prompt);
        return RunFunctionAsync(function, config.Parameters);
    }

    public Task RunFunctionAsync(Function function, Dictionary<string, string>? parameters = default)
    {
        if (parameters != null)
        {
            foreach (var parameter in parameters)
            {
                Context.Variables[parameter.Key] = parameter.Value;
            }
        }

        return function.RunAsync(function.RequestSettings);
    }

    public Task<SemanticAnswer> RunTextCompletionAsync(string prompt)
    {
        var requestSetting = AIRequestSettings.FromCompletionConfig(PromptTemplateConfig.Completion);
        return TextCompletionService.RunTextCompletionAsync(prompt, requestSetting);
    }

    public Task<ChatHistory> RunChatCompletionAsync(string systemMessage)
    {
        var chatHistory = TextCompletionService.CreateNewChat(systemMessage);
        var requestSetting = AIRequestSettings.FromCompletionConfig(PromptTemplateConfig.Completion);
        return TextCompletionService.RunChatCompletionAsync(chatHistory, requestSetting);
    }

    public Task<ChatHistory> RunChatCompletionAsync(ChatHistory history)
    {
        var requestSetting = AIRequestSettings.FromCompletionConfig(PromptTemplateConfig.Completion);
        return TextCompletionService.RunChatCompletionAsync(history, requestSetting);
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

    public async Task<bool> SaveMemoryFromPdfDirectory(string directory)
    {
        Verify.NotNull(_memory);
        return await Embedding.SaveFromPdfDirectory(_memory, directory).ConfigureAwait(false);
    }

    public async Task<bool> SaveMemoryAsync(Dictionary<string, string> textData)
    {
        Verify.NotNull(_memory); 
        return await Embedding.SaveAsync(_memory, textData).ConfigureAwait(false);
    }

    public async Task<IAsyncEnumerable<MemoryQueryResult>> SearchMemoryAsync(string query)
    {
        Verify.NotNull(_memory);
        return await Embedding.SearchAsync(_memory, query).ConfigureAwait(false);
    }

    public void UseMemory(IEmbeddingGeneration embeddingService, IMemoryStore storage)
    {
        Verify.NotNull(storage);
        Verify.NotNull(embeddingService);

        EmbeddingService = embeddingService;
        _memory = new SemanticMemory(embeddingService, storage);
    }

    public async Task<string?> RunImageGenerationAsync(string description, int width, int height)
    {
        Verify.NotNull(ImageGenerationService);
        return await ImageGenerationService.GenerateImageAsync(description, width, height).ConfigureAwait(false);
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

    public async Task<Plan?> RunPlanAsync(string goal, WorkerTypeKind workerType)
    {
        Verify.NotNullOrWhitespace(goal);

        Worker planner = new ActionPlanWorker(goal);
        Plan? plan;
        try
        {
            switch (workerType)
            {
                case WorkerTypeKind.Sequential:
                    planner = new SequentialPlanWorker(goal);
                    break;
                case WorkerTypeKind.Stepwise:
                    planner = new StepwisePlanWorker(goal);
                    break;
            }
            await planner.BuildPlanAsync().ConfigureAwait(false);
            plan = await planner.RunPlanAsync();
        }
        catch (SKException e)
        {
            Console.WriteLine(e);
            throw;
        }

        return plan;
    }

    private Function CreateSemanticFunction(SemanticFunctionConfig config)
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

    public Function RegisterSemanticFunction(SemanticFunctionConfig config)
    {
        var pluginName = config.PluginName;
        Verify.ValidPluginName(pluginName);
        Verify.ValidFunctionName(config.FunctionName);

        Function function = CreateSemanticFunction(config);

        if (!Plugins.TryGetValue(pluginName, out var plugin))
        {
            plugin = new Plugin(pluginName);
            Plugins.Add(pluginName, plugin);
        }

        plugin.AddFunction(function);

        return function;
    }

    public void RegisterNativeFunction(Function function)
    {
        var pluginName = function.PluginName;
        if (!Plugins.TryGetValue(pluginName, out var plugin))
        {
            plugin = new Plugin(pluginName);
            Plugins.Add(pluginName, plugin);
        }

        plugin.AddFunction(function);
    }

    public Function? FindFunction(string pluginName, string functionName)
    {
        Verify.ValidPluginName(pluginName);
        Verify.ValidFunctionName(functionName);

        Plugins.TryGetValue(pluginName, out var plugin);
        if (plugin != null)
            return plugin.GetFunction(functionName);
        return null;
    }

    public string BuildFunctionViews()
    {
        var result = string.Empty;
        foreach (var plugin in Plugins.Values)
        {
            if(!_includedFunctionViews.Contains(plugin.Name))
                continue;

            foreach (var function in plugin.Functions)
            {
                result += string.Join("\n\n", function.View.ToManualString());
                result += "\n\n";
            }
        }

        return result;
    }

    public void AddIncludedFunctionView(string[] pluginNames)
    {
        _includedFunctionViews.AddRange(pluginNames);
    }
}


