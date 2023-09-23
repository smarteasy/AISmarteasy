using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SemanticKernel.Connector.OpenAI;
using SemanticKernel.Connector.OpenAI.TextCompletion;
using SemanticKernel.Context;
using SemanticKernel.Function;
using SemanticKernel.Handler;
using SemanticKernel.Memory;
using SemanticKernel.Prompt;
using SemanticKernel.Service;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SemanticKernel;

public sealed class Kernel : IKernel, IDisposable
{
    private const string SEMANTIC_PLUGIN_CONFIG_FILE = "config.json";
    private const string SEMANTIC_PLUGIN_PROMPT_FILE = "skprompt.txt";
    private readonly string SEMANTIC_PLUGIN_DIRECTORY;

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
        SEMANTIC_PLUGIN_DIRECTORY = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "plugins", "semantic"); ;

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

        Context = new SKContext(
            plugins: _pluginCollection,
            loggerFactory: loggerFactory);
    }

    public Task<SemanticAnswer> RunCompletion(string prompt)
    {
        var requestSetting = CompleteRequestSettings.FromCompletionConfig(PromptTemplateConfig.Completion);
        return AIService.RunCompletion(prompt, requestSetting);
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

        var answer= await function.InvokeAsync(requestSetting);

        var result = new SemanticAnswer(answer.Result);
        return result;
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

    private ISKFunction CreateSemanticFunction(
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

        func.SetDefaultSkillCollection(Plugins);

        func.SetAIConfiguration(CompleteRequestSettings.FromCompletionConfig(functionConfig.PromptTemplateConfig.Completion));

        return func;
    }

    private void LoadSemanticPlugin()
    {
        string[] subDirectories = Directory.GetDirectories(SEMANTIC_PLUGIN_DIRECTORY);
        LoadSemanticPlugin(subDirectories);
    }

    private void LoadSemanticPlugin(string[] subDirectories)
    {
        LoadSemanticSubPlugin(SEMANTIC_PLUGIN_DIRECTORY, subDirectories);
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

    public ISKFunction RegisterCustomFunction(ISKFunction customFunction)
    {
        Verify.NotNull(customFunction);

        customFunction.SetDefaultSkillCollection(Plugins);
        _pluginCollection.AddFunction(customFunction);

        return customFunction;
    }

    public void RegisterMemory(ISemanticTextMemory memory)
    {
        _memory = memory;
    }

    public void Dispose()
    {
        if (_memory is IDisposable mem) { mem.Dispose(); }
        if (_pluginCollection is IDisposable reg) { reg.Dispose(); }
    }
}
