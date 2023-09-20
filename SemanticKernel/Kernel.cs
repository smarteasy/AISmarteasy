using Microsoft.Extensions.Logging;
using SemanticKernel.Connector.OpenAI;
using SemanticKernel.Connector.OpenAI.TextCompletion;
using SemanticKernel.Context;
using SemanticKernel.Function;
using SemanticKernel.Handler;
using SemanticKernel.Memory;
using SemanticKernel.Prompt;
using SemanticKernel.Service;

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

        LoadPlugin();

        Context = new SKContext(
            plugins: _pluginCollection,
            loggerFactory: loggerFactory);
    }

    public Task<SemanticAnswer> RunCompletion(string prompt)
    {
        var requestSetting = CompleteRequestSettings.FromCompletionConfig(PromptTemplateConfig.Completion);
        return AIService.RunCompletion(prompt, requestSetting);
    }

    public async Task<SemanticAnswer> RunFunction(IKernel kernel, ISKFunction function,
        IDictionary<string, string> parameters)
    {
        var requestSetting = CompleteRequestSettings.FromCompletionConfig(PromptTemplateConfig.Completion);
        
        foreach (var parameter in parameters)
        {
            Context.Variables[parameter.Key] = parameter.Value;
        }

        var answer= await function.InvokeAsync(kernel, requestSetting);

        var result = new SemanticAnswer(answer.Result);
        return result;
    }

    public void RegisterSemanticFunction(string functionName, SemanticFunctionConfig functionConfig)
    {
        RegisterSemanticFunction(PluginCollection.GlobalPlugin, functionName, functionConfig);
    }

    public void RegisterNativeFunction(ISKFunction function)
    {
        _pluginCollection.AddFunction(function);
    }

    public void RegisterSemanticFunction(string pluginName, string functionName, SemanticFunctionConfig functionConfig)
    {
        Verify.ValidPluginName(pluginName);
        Verify.ValidFunctionName(functionName);

        ISKFunction function = CreateSemanticFunction(pluginName, functionName, functionConfig);
        _pluginCollection.AddFunction(function);
    }

    public ISKFunction RegisterCustomFunction(ISKFunction customFunction)
    {
        Verify.NotNull(customFunction);

        customFunction.SetDefaultSkillCollection(this.Plugins);
        _pluginCollection.AddFunction(customFunction);

        return customFunction;
    }

    public void RegisterMemory(ISemanticTextMemory memory)
    {
        _memory = memory;
    }

    public Task<SKContext> RunAsync(ISKFunction skFunction,
        ContextVariables? variables = null,
        CancellationToken cancellationToken = default)
        => this.RunAsync(variables ?? new(), cancellationToken, skFunction);

    public Task<SKContext> RunAsync(params ISKFunction[] pipeline)
        => this.RunAsync(new ContextVariables(), pipeline);

    public Task<SKContext> RunAsync(string input, params ISKFunction[] pipeline)
        => this.RunAsync(new ContextVariables(input), pipeline);

    public Task<SKContext> RunAsync(ContextVariables variables, params ISKFunction[] pipeline)
        => this.RunAsync(variables, CancellationToken.None, pipeline);

    public Task<SKContext> RunAsync(CancellationToken cancellationToken, params ISKFunction[] pipeline)
        => this.RunAsync(new ContextVariables(), cancellationToken, pipeline);

    public Task<SKContext> RunAsync(string input, CancellationToken cancellationToken, params ISKFunction[] pipeline)
        => this.RunAsync(new ContextVariables(input), cancellationToken, pipeline);

    public async Task<SKContext> RunAsync(ContextVariables variables, CancellationToken cancellationToken, params ISKFunction[] pipeline)
    {
        var context = new SKContext(
            variables,
            _pluginCollection,
            LoggerFactory);

        int pipelineStepCount = 0;

        foreach (ISKFunction f in pipeline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                //context = await f.InvokeAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Plugin {Plugin} function {Function} call fail during pipeline step {Step} with error {Error}:", f.PluginName, f.Name, pipelineStepCount, ex.Message);
                throw;
            }

            pipelineStepCount++;
        }

        return context;
    }

    public ISKFunction Func(string pluginName, string functionName)
    {
        return this.Plugins.GetFunction(pluginName, functionName);
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


    private IDictionary<string, ISKFunction> LoadPlugin()
    {
        LoadSemanticPlugin();


        var plugin = new Dictionary<string, ISKFunction>();

        ILogger? logger = null;
        //foreach (var pluginDirectoryName in pluginDirectoryNames)
        //{
        //Verify.ValidSkillName(pluginDirectoryName);
        //var pluginDirectory = Path.Combine(pluginsDirectory, pluginDirectoryName);
        //Verify.DirectoryExists(pluginDirectory);




        //kernel.CreateNewContext();

        return plugin;
    }

    private void LoadSemanticPlugin()
    {
        string[] subDirectories = Directory.GetDirectories(SEMANTIC_PLUGIN_DIRECTORY);
        LoadSemanticPlugin(subDirectories);
    }

    private void LoadSemanticPlugin(string[] subDirectories)
    {
        foreach (var subDirectory in subDirectories)
        {
            var directoryName = Path.GetFileName(subDirectory);
            LoadSemanticSubPlugin(directoryName, Directory.GetDirectories(subDirectory));
        }
    }

    private void LoadSemanticSubPlugin(string parentDirectoryName, string[] subDirectories)
    {
        foreach (var subDirectory in subDirectories)
        {
            var directoryName = Path.GetFileName(subDirectory);
            var promptPath = Path.Combine(subDirectory, SEMANTIC_PLUGIN_PROMPT_FILE);
            if (File.Exists(promptPath))
            {
                LoadSemanticFunction(subDirectory, parentDirectoryName, directoryName);
            }
            else
            {
                LoadSemanticSubPlugin(parentDirectoryName, Directory.GetDirectories(subDirectory));
            }
        }
    }

    private void LoadSemanticFunction(string directoryPath, string pluginName, string functionName)
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

    public void Dispose()
    {
        if (_memory is IDisposable mem) { mem.Dispose(); }
        if (_pluginCollection is IDisposable reg) { reg.Dispose(); }
    }
}
