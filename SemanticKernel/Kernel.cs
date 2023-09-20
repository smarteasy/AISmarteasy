using System.Reflection;
using System.Threading;
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
    private readonly IPluginCollection _pluginCollection;
    private ISemanticTextMemory _memory;
    private readonly ILogger _logger;

    public ISemanticTextMemory Memory => _memory;

    public IPromptTemplate PromptTemplate { get; }

    public PromptTemplateConfig PromptTemplateConfig { get; }

    public ILoggerFactory LoggerFactory { get; }


    public IReadOnlyPluginCollection Plugins => _pluginCollection;

    public IDelegatingHandlerFactory HttpHandlerFactory { get; }

    public IAIService AIService { get; }
    public SKContext Context { get; set; }

    public Kernel(IAIService aiService, 
        ISemanticTextMemory memory, IDelegatingHandlerFactory httpHandlerFactory,
        ILoggerFactory loggerFactory)
    {
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

        ImportPluginFromDirectory();

        Context = new SKContext(
            plugins: _pluginCollection,
            loggerFactory: loggerFactory);
    }

    public IDictionary<string, ISKFunction> ImportPluginFromDirectory()
    {
        const string ConfigFile = "config.json";
        const string PromptFile = "skprompt.txt";

        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "plugins");
        var plugin = new Dictionary<string, ISKFunction>();

        ILogger? logger = null;
        //foreach (var pluginDirectoryName in pluginDirectoryNames)
        //{
            //Verify.ValidSkillName(pluginDirectoryName);
            //var pluginDirectory = Path.Combine(pluginsDirectory, pluginDirectoryName);
            //Verify.DirectoryExists(pluginDirectory);

            string[] directories = Directory.GetDirectories(pluginsDirectory);
            foreach (string directory in directories)
            {
                var directoryName = Path.GetFileName(directory);
                var promptPath = Path.Combine(directory, PromptFile);

                if (!File.Exists(promptPath))
                {
                    ImportPlugin(directoryName, directory);
                    continue;
                }

                var config = new PromptTemplateConfig();
                var configPath = Path.Combine(directory, ConfigFile);
                if (File.Exists(configPath))
                {
                    config = PromptTemplateConfig.FromJson(File.ReadAllText(configPath));
                }

                //logger ??= LoggerFactory.CreateLogger(typeof(IKernel));
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace("Config {0}: {1}", directoryName, config.ToJson());
                }

                var template = new PromptTemplate(File.ReadAllText(promptPath), config);
                var functionConfig = new SemanticFunctionConfig(config, template);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace("Registering function {0}.{1} loaded from {2}", directory, directoryName,
                        directory);
                }

                //plugin[functionName] =
                RegisterSemanticFunction(directory, directoryName, functionConfig);
                //}
            }

            //kernel.CreateNewContext();

        return plugin;
    }

    private void ImportPlugin(string pluginName, string directory)
    {
        const string ConfigFile = "config.json";
        const string PromptFile = "skprompt.txt";

        ILogger? logger = null;

        string[] subDirectories = Directory.GetDirectories(directory);
        foreach (var subDirectory in subDirectories)
        {
            var functionName = Path.GetFileName(subDirectory);
            var promptPath = Path.Combine(subDirectory, PromptFile);

            if (!File.Exists(promptPath))
            {
                ImportPlugin(subDirectory);
                continue;
            }

            var config = new PromptTemplateConfig();
            var configPath = Path.Combine(subDirectory, ConfigFile);
            if (File.Exists(configPath))
            {
                config = PromptTemplateConfig.FromJson(File.ReadAllText(configPath));
            }

            logger ??= LoggerFactory.CreateLogger(typeof(IKernel));
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace("Config {0}: {1}", functionName, config.ToJson());
            }

            var template = new PromptTemplate(File.ReadAllText(promptPath), config);
            var functionConfig = new SemanticFunctionConfig(config, template);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace("Registering function {0}.{1} loaded from {2}", subDirectory, functionName,
                    subDirectory);
            }

            RegisterSemanticFunction(pluginName, functionName, functionConfig);
        }
    }

    public Task<SemanticAnswer> RunCompletion(string prompt)
    {
        var requestSetting = CompleteRequestSettings.FromCompletionConfig(PromptTemplateConfig.Completion);
        return AIService.RunCompletion(prompt, requestSetting);
    }

    public async Task<SemanticAnswer> RunSemanticFunction(IKernel kernel, ISKFunction function,
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

    public ISKFunction RegisterSemanticFunction(string functionName, SemanticFunctionConfig functionConfig)
    {
        return RegisterSemanticFunction(PluginCollection.GlobalPlugin, functionName, functionConfig);
    }

    public ISKFunction RegisterSemanticFunction(string pluginName, string functionName, SemanticFunctionConfig functionConfig)
    {
        Verify.ValidSkillName(pluginName);
        Verify.ValidFunctionName(functionName);

        ISKFunction function = CreateSemanticFunction(pluginName, functionName, functionConfig);
        _pluginCollection.AddFunction(function);

        return function;
    }
    public IDictionary<string, ISKFunction> ImportPlugin(object pluginInstance, string? pluginName = null)
    {
        Verify.NotNull(pluginInstance);

        if (string.IsNullOrWhiteSpace(pluginName))
        {
            pluginName = PluginCollection.GlobalPlugin;
            _logger.LogTrace("Importing plugin {0} in the global namespace", pluginInstance.GetType().FullName);
        }
        else
        {
            _logger.LogTrace("Importing plugin {0}", pluginName);
        }

        Dictionary<string, ISKFunction> skill = ImportPlugin(
            pluginInstance,
            pluginName!,
            _logger,
            LoggerFactory
        );
        foreach (KeyValuePair<string, ISKFunction> f in skill)
        {
            f.Value.SetDefaultSkillCollection(this.Plugins);
            this._pluginCollection.AddFunction(f.Value);
        }

        return skill;
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

    public ISKFunction Func(string skillName, string functionName)
    {
        return this.Plugins.GetFunction(skillName, functionName);
    }

    public void Dispose()
    {
        if (_memory is IDisposable mem) { mem.Dispose(); }
        if (_pluginCollection is IDisposable reg) { reg.Dispose(); }
    }

    private ISKFunction CreateSemanticFunction(
        string skillName,
        string functionName,
        SemanticFunctionConfig functionConfig)
    {
        if (!functionConfig.PromptTemplateConfig.Type.Equals("completion", StringComparison.OrdinalIgnoreCase))
        {
            throw new SKException($"Function type not supported: {functionConfig.PromptTemplateConfig}");
        }

        ISKFunction func = SemanticFunction.FromSemanticConfig(
            skillName,
            functionName,
            functionConfig,
            LoggerFactory
        );

        func.SetDefaultSkillCollection(Plugins);

        func.SetAIConfiguration(CompleteRequestSettings.FromCompletionConfig(functionConfig.PromptTemplateConfig.Completion));

        return func;
    }

   private static Dictionary<string, ISKFunction> ImportPlugin(object plugin, string pluginName, ILogger logger, ILoggerFactory loggerFactory)
    {
        MethodInfo[] methods = plugin.GetType().GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
        logger.LogTrace("Importing skill name: {0}. Potential methods found: {1}", pluginName, methods.Length);

        Dictionary<string, ISKFunction> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (MethodInfo method in methods)
        {
            if (method.GetCustomAttribute<SKFunctionAttribute>() is not null)
            {
                ISKFunction function = SKFunction.FromNativeMethod(method, plugin, pluginName, loggerFactory);
                if (result.ContainsKey(function.Name))
                {
                    throw new SKException("Function overloads are not supported, please differentiate function names");
                }

                result.Add(function.Name, function);
            }
        }

        logger.LogTrace("Methods imported {0}", result.Count);

        return result;
    }
}
