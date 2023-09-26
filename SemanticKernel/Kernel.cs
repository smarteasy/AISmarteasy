using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SemanticKernel.Connector.OpenAI;
using SemanticKernel.Connector.OpenAI.TextCompletion;
using SemanticKernel.Context;
using SemanticKernel.Function;
using SemanticKernel.Handler;
using SemanticKernel.Memory;
using SemanticKernel.Planner;
using SemanticKernel.Prompt;
using SemanticKernel.Service;

namespace SemanticKernel;

public sealed class Kernel : IKernel, IDisposable
{
    private const string SEMANTIC_PLUGIN_CONFIG_FILE = "config.json";
    private const string SEMANTIC_PLUGIN_PROMPT_FILE = "skprompt.txt";
    private const string AVAILABLE_FUNCTIONS_KEY = "available_functions";

    private readonly string _semanticPluginDirectory;

    private readonly IPluginCollection _pluginCollection;

    private ISemanticTextMemory _memory;
    private readonly ILogger _logger;

    public IReadOnlyPluginCollection Plugins => _pluginCollection;


    public ISemanticTextMemory Memory => _memory;

    public IPromptTemplate PromptTemplate { get; }

    public PromptTemplateConfig PromptTemplateConfig { get; }

    public ILoggerFactory LoggerFactory { get; }


    public IDelegatingHandlerFactory HttpHandlerFactory { get; }


    public IAIService AIService { get; }
    public SKContext Context { get; set; }

    public Kernel(IAIService aiService, 
        ISemanticTextMemory memory, IDelegatingHandlerFactory httpHandlerFactory,
        ILoggerFactory loggerFactory)
    {
        _semanticPluginDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "plugins", "semantic"); ;

        LoggerFactory = loggerFactory;
        _logger = LoggerFactory.CreateLogger(typeof(Kernel));

        PromptTemplate = new PromptTemplate(LoggerFactory);
        PromptTemplateConfig = PromptTemplateConfigBuilder.Build();

        //TODO - 내용 정리
        _pluginCollection = new PluginCollection(LoggerFactory);
        AIService = aiService;

        //TODO - 세부적인 처리 과정 추적

        HttpHandlerFactory = httpHandlerFactory;
        _memory = memory;


        LoadSemanticPlugin(); 

        Context = new SKContext(loggerFactory: loggerFactory);
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

        var context = await function.InvokeAsync(requestSetting);

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

    public ISKFunction CreateSemanticFunction(
        string pluginName,
        string functionName,
        SemanticFunctionConfig functionConfig)
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

        var functionsManual = BuildFunctionsManual();

        var parameters = new Dictionary<string, string>
        {
            { AVAILABLE_FUNCTIONS_KEY, functionsManual },
            { "input", goal }
        };

        var planner = Plugins.GetFunction("OrchestratorSkill", "SequencePlanner");
        var answerPlan = await RunFunction(planner, parameters).ConfigureAwait(false);

        var planText  = answerPlan.Text.Trim();

        if (string.IsNullOrWhiteSpace(planText))
        {
            throw new SKException(
                "Unable to create plan. No response from Function Flow function. " +
                $"\nGoal:{goal}\nFunctions:\n{planText}");
        }

        var functions = _pluginCollection.GetAllFunctions();

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

    private static string BuildFunctionsManual()
    {
        var functionsView = KernelProvider.Kernel.Plugins.GetFunctionsView();
        var result = string.Empty;
        foreach (var functionViews in functionsView.FunctionViews.Values)
        {
            result += string.Join("\n\n", functionViews.Select(x => x.ToManualString()));
            result += "\n\n";
        }

        return result;
    }


    public void CreateSemanticFunction(string promptTemplate, string pluginName, string functionName, string? description = null, 
        AIRequestSettings? requestSettings = null)
    {
        functionName ??= RandomFunctionName();

        var config = new PromptTemplateConfig
        {
            Description = description ?? "Generic function, unknown purpose",
            Type = "completion",
            //Completion = CompleteRequestSettings.FromCompletionConfig(functionConfig.PromptTemplateConfig.Completion));
    };

        functionName ??= RandomFunctionName();
        Verify.ValidFunctionName(functionName);
        if (!string.IsNullOrEmpty(pluginName)) { Verify.ValidPluginName(pluginName); }

        var template = new PromptTemplate(promptTemplate, config);
        var functionConfig = new SemanticFunctionConfig(config, template);

        RegisterSemanticFunction(pluginName!, functionName, functionConfig);
    }


    private void LoadSemanticPlugin()
    {
        string[] subDirectories = Directory.GetDirectories(_semanticPluginDirectory);
        LoadSemanticPlugin(subDirectories);
    }

    private void LoadSemanticPlugin(string[] subDirectories)
    {
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
        _pluginCollection.AddFunction(function);
    }

    public void RegisterNativeFunction(ISKFunction function)
    {
        _pluginCollection.AddFunction(function);
    }

    public ISKFunction RegisterCustomFunction(ISKFunction function)
    {
        Verify.NotNull(function);

        function.SetDefaultPluginCollection(Plugins);
        _pluginCollection.AddFunction(function);

        return function;
    }

    public void RegisterMemory(ISemanticTextMemory memory)
    {
        _memory = memory;
    }

    private static string RandomFunctionName() => "func" + Guid.NewGuid().ToString("N");

    public SKContext CreateNewContext(ContextVariables variables)
    {
        return new SKContext(variables);
    }

    public void Dispose()
    {
        if (_memory is IDisposable mem) { mem.Dispose(); }
        if (_pluginCollection is IDisposable plugins) { plugins.Dispose(); }
    }
}
