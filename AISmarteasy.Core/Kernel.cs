﻿using System.Numerics;
using AISmarteasy.Core.Config;
using AISmarteasy.Core.Connector.OpenAI;
using AISmarteasy.Core.Connector.OpenAI.TextCompletion;
using AISmarteasy.Core.Connector.OpenAI.TextCompletion.Chat;
using AISmarteasy.Core.Context;
using AISmarteasy.Core.Function;
using AISmarteasy.Core.Handler;
using AISmarteasy.Core.Memory;
using AISmarteasy.Core.Planner;
using AISmarteasy.Core.Prompt;
using AISmarteasy.Core.Service;
using AISmarteasy.Core.Text;
using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core;

public sealed class Kernel : IDisposable
{
    private const string SEMANTIC_PLUGIN_CONFIG_FILE = "config.json";
    private const string SEMANTIC_PLUGIN_PROMPT_FILE = "skprompt.txt";


    private readonly string _semanticPluginDirectory;

    private readonly ILogger _logger;
    private ISemanticTextMemory? _memory;

    public Dictionary<string, Plugin> Plugins { get; }

    public IPromptTemplate PromptTemplate { get; }

    public PromptTemplateConfig PromptTemplateConfig { get; }

    public ILoggerFactory LoggerFactory { get; }

    public IDelegatingHandlerFactory HttpHandlerFactory { get; }

    public IAIService AIService { get; }

    public IAIService? EmbeddingService { get; private set; }

    public SKContext Context { get; set; }

    public Kernel(IAIService aiService, IDelegatingHandlerFactory httpHandlerFactory, ILoggerFactory loggerFactory)
    {
        AIService = aiService;
        HttpHandlerFactory = httpHandlerFactory;
        LoggerFactory = loggerFactory;

        Context = new SKContext(loggerFactory: loggerFactory);
        _logger = LoggerFactory.CreateLogger(typeof(Kernel));

        PromptTemplate = new PromptTemplate(LoggerFactory);
        PromptTemplateConfig = PromptTemplateConfigBuilder.Build();

        Plugins = new Dictionary<string, Plugin>();

        _semanticPluginDirectory = Path.Combine(Directory.GetCurrentDirectory(), "plugins", "semantic"); ;
        LoadSemanticPlugin(); 
    }

    public string ContextVariablesInput => KernelProvider.Kernel.Context.Variables.Input;

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

        var requestSetting = CompleteRequestSettings.FromCompletionConfig(PromptTemplateConfig.Completion);
        return function.InvokeAsync(requestSetting);
    }








    public async Task<List<string>> SaveEmbeddingsFromDirectoryPdfFiles(string directory)
    {
        if (_memory != null) return await PdfLoader.SaveEmbeddingsFromDirectoryFiles(_memory, directory).ConfigureAwait(false);
        return new List < string>();
    }

    public async Task<string?> SaveInformationAsync(string collection, string text, string id,
        string? description = null, string? additionalMetadata = null)

    {
        if (_memory != null) 
            return await _memory.SaveInformationAsync(collection, text, id).ConfigureAwait(false);

        return null;
    }

    public void UseMemory(IAIService embeddingService, IMemoryStore storage)
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
                var requestSetting = CompleteRequestSettings.FromCompletionConfig(PromptTemplateConfig.Completion);
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





    public Task<ChatHistory> RunChatCompletion(ChatHistory history)
    {
        var requestSetting = CompleteRequestSettings.FromCompletionConfig(PromptTemplateConfig.Completion);
        return AIService.RunChatCompletion(history, requestSetting);
    }

    public Task<SemanticAnswer> RunCompletion(string prompt)
    {
        var requestSetting = CompleteRequestSettings.FromCompletionConfig(PromptTemplateConfig.Completion);
        return AIService.RunTextCompletion(prompt, requestSetting);
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

        func.SetAIConfiguration(CompleteRequestSettings.FromCompletionConfig(config.PromptTemplateConfig.Completion));

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
            _logger.LogTrace("Config {0}: {1}", functionName, config.ToJson());
            _logger.LogTrace("Registering function {0}.{1}", pluginName, functionName);
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
    
    public void RegisterMemory(IAIService embeddingService, IMemoryStore storage)
    {
        EmbeddingService = embeddingService;
        _memory = new SemanticTextMemory(embeddingService, storage);
    }

    public SKContext CreateNewContext(ContextVariables variables)
    {
        return new SKContext(variables);
    }





    public void Dispose()
    {
        if (_memory is IDisposable mem) { mem.Dispose(); }
        if (Plugins is IDisposable plugins) { plugins.Dispose(); }
    }
}
