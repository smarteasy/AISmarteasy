using System.Reflection;
using Microsoft.Extensions.Logging;
using SemanticKernel.Context;
using SemanticKernel.Exception;
using SemanticKernel.Function;
using SemanticKernel.Handler;
using SemanticKernel.Memory;
using SemanticKernel.Prompt;
using SemanticKernel.Service;

namespace SemanticKernel;

public sealed class Kernel : IKernel, IDisposable
{
        public ILoggerFactory LoggerFactory { get; }

        public ISemanticTextMemory Memory => this._memory;

    public IReadOnlySkillCollection Skills => this._skillCollection;

    public IPromptTemplateEngine PromptTemplateEngine { get; }

    public static KernelBuilder Builder => new();
    public IDelegatingHandlerFactory HttpHandlerFactory { get; }

    public Kernel(
       ISkillCollection skillCollection,
       IAIServiceProvider aiServiceProvider,
       IPromptTemplateEngine promptTemplateEngine,
       ISemanticTextMemory memory,
       IDelegatingHandlerFactory httpHandlerFactory,
       ILoggerFactory loggerFactory)
    {
        LoggerFactory = loggerFactory;
        HttpHandlerFactory = httpHandlerFactory;
        PromptTemplateEngine = promptTemplateEngine;
        _memory = memory;
        _aiServiceProvider = aiServiceProvider;
        _promptTemplateEngine = promptTemplateEngine;
        _skillCollection = skillCollection;

        _logger = loggerFactory.CreateLogger(typeof(Kernel));
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
                context = await f.InvokeAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);
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

    public T GetService<T>(string? name = null) where T : IAIService
    {
        var service = _aiServiceProvider.GetService<T>(name);
        if (service != null)
        {
            return service;
        }

        throw new SKException($"Service of type {typeof(T)} and name {name ?? "<NONE>"} not registered.");
    }

    public void Dispose()
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (_memory is IDisposable mem) { mem.Dispose(); }

        // ReSharper disable once SuspiciousTypeConversion.Global
        if (_skillCollection is IDisposable reg) { reg.Dispose(); }
    }


    private readonly ISkillCollection _skillCollection;
    private ISemanticTextMemory _memory;
    private readonly IPromptTemplateEngine _promptTemplateEngine;
    private readonly IAIServiceProvider _aiServiceProvider;
    private readonly ILogger _logger;

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

        func.SetAIService(() => this.GetService<ITextCompletion>(functionConfig.PromptTemplateConfig.Completion.ServiceId));

        return func;
    }

   private static Dictionary<string, ISKFunction> ImportSkill(object skillInstance, string skillName, ILogger logger, ILoggerFactory loggerFactory)
    {
        MethodInfo[] methods = skillInstance.GetType().GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
        logger.LogTrace("Importing skill name: {0}. Potential methods found: {1}", skillName, methods.Length);

        // Filter out non-SKFunctions and fail if two functions have the same name
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
