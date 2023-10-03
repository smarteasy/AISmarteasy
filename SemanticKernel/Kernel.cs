using Microsoft.Extensions.Logging;
using SemanticKernel.Connector.OpenAI;
using SemanticKernel.Connector.OpenAI.TextCompletion;
using SemanticKernel.Connector.OpenAI.TextCompletion.Chat;
using SemanticKernel.Context;
using SemanticKernel.Function;
using SemanticKernel.Handler;
using SemanticKernel.Memory;
using SemanticKernel.Planner;
using SemanticKernel.Prompt;
using SemanticKernel.Service;

namespace SemanticKernel;

public sealed class Kernel : IDisposable
{
    private const string SEMANTIC_PLUGIN_CONFIG_FILE = "config.json";
    private const string SEMANTIC_PLUGIN_PROMPT_FILE = "skprompt.txt";
    private const string AVAILABLE_FUNCTIONS_KEY = "available_functions";

    private readonly string _semanticPluginDirectory;

    private readonly ILogger _logger;

    public Dictionary<string, Plugin> Plugins { get; }

    public ISemanticTextMemory? Memory { get; private set; } = null;

    public IPromptTemplate PromptTemplate { get; }

    public PromptTemplateConfig PromptTemplateConfig { get; }

    public ILoggerFactory LoggerFactory { get; }

    public IDelegatingHandlerFactory HttpHandlerFactory { get; }

    public IAIService AIService { get; }
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

        _semanticPluginDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "plugins", "semantic"); ;
        LoadSemanticPlugin(); 
    }

    public void UseMemory(IAIService embeddingGenerator, IMemoryStore storage)
    {
        Verify.NotNull(storage);
        Verify.NotNull(embeddingGenerator);
        RegisterMemory(embeddingGenerator, storage);
    }

    public async Task<Plan> RunPlan(string prompt)
    {
        var plan = await CreatePlanAsync(prompt);

        while (plan.HasNextStep)
        {
            KernelProvider.Kernel.Context = KernelProvider.Kernel.CreateNewContext(new ContextVariables(KernelProvider.Kernel.Context.Variables.Input));
            await plan.RunNextStepAsync();
        }

        return plan;
    }

    public Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddings(IList<string> data, CancellationToken cancellationToken = default)
    {
        return AIService.GenerateEmbeddings(data, cancellationToken);
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

    public Task<SemanticAnswer> RunFunction(ISKFunction function)
    {
        return RunFunction(function, new Dictionary<string, string>());
    }

    public async Task<SemanticAnswer> RunFunction(ISKFunction function, IDictionary<string, string> parameters)
    {
        var requestSetting = CompleteRequestSettings.FromCompletionConfig(PromptTemplateConfig.Completion);
        
        foreach (var parameter in parameters)
        {
            Context.Variables[parameter.Key] = parameter.Value;
        }

        var context = await function.InvokeAsync(Context, requestSetting);

        return new SemanticAnswer(context.Variables.Input);
    }


    public async Task<SemanticAnswer> RunPipeline(params ISKFunction[] pipeline)
    {
        var variables = Context.Variables;
        int pipelineStepCount = 0;
        SemanticAnswer answer = new SemanticAnswer(string.Empty);

        foreach (ISKFunction function in pipeline)
        {
            try
            {
                answer = await RunFunction(function);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Plugin {Plugin} function {Function} call fail during pipeline step {Step} with error {Error}:", function.PluginName, function.Name, pipelineStepCount, ex.Message);
                throw;
            }

            pipelineStepCount++;
        }

        var result = new SemanticAnswer(answer.Text);
        return result;
    }

    public ISKFunction CreateSemanticFunction(string pluginName, string functionName, SemanticFunctionConfig functionConfig)
    {
        if (!functionConfig.PromptTemplateConfig.Type.Equals("completion", StringComparison.OrdinalIgnoreCase))
        {
            throw new SKException($"Function type not supported: {functionConfig.PromptTemplateConfig}");
        }

        ISKFunction func = SemanticFunction.FromSemanticConfig(
            pluginName,
            functionName,
            functionConfig,
            LoggerFactory
        );

        func.SetAIConfiguration(CompleteRequestSettings.FromCompletionConfig(functionConfig.PromptTemplateConfig.Completion));

        return func;
    }

    public async Task<Plan> CreatePlanAsync(string goal, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(goal))
        {
            throw new SKException("The goal specified is empty");
        }

        var functionViews = BuildFunctionViews();

        var parameters = new Dictionary<string, string>
        {
            { AVAILABLE_FUNCTIONS_KEY, functionViews },
            { "input", goal }
        };

        Plugins.TryGetValue("OrchestratorSkill", out var plugin);
        var planner = plugin!.GetFunction("SequencePlanner");

        var answerPlan = await RunFunction(planner, parameters).ConfigureAwait(false);

        var planText  = answerPlan.Text.Trim();

        if (string.IsNullOrWhiteSpace(planText))
        {
            throw new SKException(
                "Unable to create plan. No response from Function Flow function. " +
                $"\nGoal:{goal}\nFunctions:\n{planText}");
        }

        Plan plan;
        try
        {
            plan = planText!.ToPlanFromXml(goal);
        }
        catch (SKException e)
        {
            throw new SKException($"Unable to create plan for goal with available functions.\nGoal:{goal}\nFunctions:\n{planText}", e);
        }

        if (plan.Steps.Count == 0)
        {
            throw new SKException($"Not possible to create plan for goal with available functions.\nGoal:{goal}\nFunctions:\n{planText}");
        }
        
        return plan;
    }

    private string BuildFunctionViews()
    {
        var result = string.Empty;
        foreach (var plugin in Plugins.Values)
        {
            result += string.Join("\n\n", plugin.BuildPluginView().FunctionViews.Values.Select(x => x.ToManualString()));
            result += "\n\n";
        }

        return result;
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
        var functionConfig = new SemanticFunctionConfig(config, template);

        RegisterSemanticFunction(pluginName, functionName, functionConfig);

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Config {0}: {1}", functionName, config.ToJson());
            _logger.LogTrace("Registering function {0}.{1}", pluginName, functionName);
        }
    }

    public void RegisterSemanticFunction(string pluginName, string functionName, SemanticFunctionConfig functionConfig)
    {
        Verify.ValidPluginName(pluginName);
        Verify.ValidFunctionName(functionName);

        ISKFunction function = CreateSemanticFunction(pluginName, functionName, functionConfig);

        if (!Plugins.TryGetValue(pluginName, out var plugin))
        {
            plugin = new Plugin(pluginName);
            Plugins.Add(pluginName, plugin);
        }

        plugin!.AddFunction(function);
    }

    public void RegisterNativeFunction(ISKFunction function)
    {
        var pluginName = function.PluginName;
        if (!Plugins.TryGetValue(pluginName, out var plugin))
        {
            plugin = new Plugin(pluginName);
            Plugins.Add(pluginName, plugin);
        }

        plugin!.AddFunction(function);
    }
    
    public void RegisterMemory(IAIService embeddingGenerator, IMemoryStore storage)
    {
        Memory = new SemanticTextMemory(embeddingGenerator, storage);
    }

    public SKContext CreateNewContext(ContextVariables variables)
    {
        return new SKContext(variables);
    }

    public ISKFunction FindFunction(string pluginName, string functionName)
    {
        Verify.ValidPluginName(pluginName);
        Verify.ValidFunctionName(functionName);

        Plugins.TryGetValue(pluginName, out var plugin);
        return plugin!.GetFunction(functionName);
    }

    public void Dispose()
    {
        if (Memory is IDisposable mem) { mem.Dispose(); }

        if (Plugins is IDisposable plugins) { plugins.Dispose(); }
    }
}
