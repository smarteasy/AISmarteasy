using System.Reflection;
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
    private readonly ISkillCollection _skillCollection;
    private ISemanticTextMemory _memory;
    private readonly ILogger _logger;

    public IPromptTemplateEngine PromptTemplateEngine { get; }

    public PromptTemplateConfig PromptTemplateConfig { get; }

    public ILoggerFactory LoggerFactory { get; }

    public ISemanticTextMemory Memory => _memory;

    public IReadOnlySkillCollection Skills => _skillCollection;

    public static KernelBuilder Builder => new();
    public IDelegatingHandlerFactory HttpHandlerFactory { get; }

    public IAIService AIService { get; }

    public Task<SemanticAnswer> RunCompletion(string prompt)
    {
        var requestSetting = CompleteRequestSettings.FromCompletionConfig(PromptTemplateConfig.Completion);
        return AIService.RunCompletion(prompt, requestSetting);
    }

    public Kernel(
        IAIService aiService,
        ISemanticTextMemory memory,
        IDelegatingHandlerFactory httpHandlerFactory,
        ILoggerFactory loggerFactory)
    {
        LoggerFactory = loggerFactory;
        _logger = LoggerFactory.CreateLogger(typeof(Kernel));

        PromptTemplateEngine = new PromptTemplateEngine(LoggerFactory);
        PromptTemplateConfig = PromptTemplateConfigBuilder.Build();

        //TODO - 내용 정리
        _skillCollection = new SkillCollection(LoggerFactory);
        AIService = aiService;

        //TODO - 세부적인 처리 과정 추적

        HttpHandlerFactory = httpHandlerFactory;
        _memory = memory;
    }

    public ISKFunction RegisterSemanticFunction(string functionName, SemanticFunctionConfig functionConfig)
    {
        return RegisterSemanticFunction(SkillCollection.GlobalSkill, functionName, functionConfig);
    }

    public ISKFunction RegisterSemanticFunction(string skillName, string functionName, SemanticFunctionConfig functionConfig)
    {
        Verify.ValidSkillName(skillName);
        Verify.ValidFunctionName(functionName);

        ISKFunction function = CreateSemanticFunction(skillName, functionName, functionConfig);
        _skillCollection.AddFunction(function);

        return function;
    }
    public IDictionary<string, ISKFunction> ImportSkill(object skillInstance, string? skillName = null)
    {
        Verify.NotNull(skillInstance);

        if (string.IsNullOrWhiteSpace(skillName))
        {
            skillName = SkillCollection.GlobalSkill;
            _logger.LogTrace("Importing skill {0} in the global namespace", skillInstance.GetType().FullName);
        }
        else
        {
            _logger.LogTrace("Importing skill {0}", skillName);
        }

        Dictionary<string, ISKFunction> skill = ImportSkill(
            skillInstance,
            skillName!,
            _logger,
            LoggerFactory
        );
        foreach (KeyValuePair<string, ISKFunction> f in skill)
        {
            f.Value.SetDefaultSkillCollection(this.Skills);
            this._skillCollection.AddFunction(f.Value);
        }

        return skill;
    }

    public ISKFunction RegisterCustomFunction(ISKFunction customFunction)
    {
        Verify.NotNull(customFunction);

        customFunction.SetDefaultSkillCollection(this.Skills);
        _skillCollection.AddFunction(customFunction);

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
            _skillCollection,
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
                _logger.LogError("Plugin {Plugin} function {Function} call fail during pipeline step {Step} with error {Error}:", f.SkillName, f.Name, pipelineStepCount, ex.Message);
                throw;
            }

            pipelineStepCount++;
        }

        return context;
    }

    public ISKFunction Func(string skillName, string functionName)
    {
        return this.Skills.GetFunction(skillName, functionName);
    }

    public SKContext CreateNewContext()
    {
        return new SKContext(
            skills: _skillCollection,
            loggerFactory: LoggerFactory);
    }


    public void Dispose()
    {
        if (_memory is IDisposable mem) { mem.Dispose(); }
        if (_skillCollection is IDisposable reg) { reg.Dispose(); }
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

        func.SetDefaultSkillCollection(this.Skills);

        func.SetAIConfiguration(CompleteRequestSettings.FromCompletionConfig(functionConfig.PromptTemplateConfig.Completion));

        return func;
    }

   private static Dictionary<string, ISKFunction> ImportSkill(object skillInstance, string skillName, ILogger logger, ILoggerFactory loggerFactory)
    {
        MethodInfo[] methods = skillInstance.GetType().GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
        logger.LogTrace("Importing skill name: {0}. Potential methods found: {1}", skillName, methods.Length);

        Dictionary<string, ISKFunction> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (MethodInfo method in methods)
        {
            if (method.GetCustomAttribute<SKFunctionAttribute>() is not null)
            {
                ISKFunction function = SKFunction.FromNativeMethod(method, skillInstance, skillName, loggerFactory);
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
