using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SemanticKernel.Context;
using SemanticKernel.Exception;
using SemanticKernel.Function;
using SemanticKernel.Handler;
using SemanticKernel.Memory;
using SemanticKernel.Prompt;
using SemanticKernel.Service;

namespace SemanticKernel;

public sealed class KernelBuilder
{
    private Func<ISemanticTextMemory> _memoryFactory = () => NullMemory.Instance;
    private ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;
    private Func<IMemoryStore>? _memoryStorageFactory = null;
    private IDelegatingHandlerFactory _httpHandlerFactory = NullHttpHandlerFactory.Instance;
    private IPromptTemplateEngine? _promptTemplateEngine;
    private readonly AIServiceCollection _aiServices = new();

    private static bool _promptTemplateEngineInitialized = false;
    private static Type? _promptTemplateEngineType = null;

    public static IKernel Create()
    {
        var builder = new KernelBuilder();
        return builder.Build();
    }

    public IKernel Build()
    {
        var instance = new Kernel(
            new SkillCollection(_loggerFactory),
            _aiServices.Build(),
            _promptTemplateEngine ?? CreateDefaultPromptTemplateEngine(_loggerFactory),
            _memoryFactory.Invoke(),
            _httpHandlerFactory,
            _loggerFactory
        );

        // TODO: decouple this from 'UseMemory' kernel extension
        if (_memoryStorageFactory != null)
        {
            instance.UseMemory(_memoryStorageFactory.Invoke());
        }

        return instance;
    }

    public KernelBuilder WithLoggerFactory(ILoggerFactory loggerFactory)
    {
        Verify.NotNull(loggerFactory);
        _loggerFactory = loggerFactory;
        return this;
    }

    public KernelBuilder WithMemory(ISemanticTextMemory memory)
    {
        Verify.NotNull(memory);
        _memoryFactory = () => memory;
        return this;
    }

    public KernelBuilder WithMemory<TStore>(Func<ILoggerFactory, TStore> factory) where TStore : ISemanticTextMemory
    {
        Verify.NotNull(factory);
        _memoryFactory = () => factory(_loggerFactory);
        return this;
    }

    public KernelBuilder WithMemoryStorage(IMemoryStore storage)
    {
        Verify.NotNull(storage);
        _memoryStorageFactory = () => storage;
        return this;
    }

    public KernelBuilder WithMemoryStorage<TStore>(Func<ILoggerFactory, TStore> factory) where TStore : IMemoryStore
    {
        Verify.NotNull(factory);
        _memoryStorageFactory = () => factory(_loggerFactory);
        return this;
    }

    public KernelBuilder WithMemoryStorage<TStore>(Func<ILoggerFactory, IDelegatingHandlerFactory, TStore> factory) where TStore : IMemoryStore
    {
        Verify.NotNull(factory);
        this._memoryStorageFactory = () => factory(this._loggerFactory, this._httpHandlerFactory);
        return this;
    }

    public KernelBuilder WithPromptTemplateEngine(IPromptTemplateEngine promptTemplateEngine)
    {
        Verify.NotNull(promptTemplateEngine);
        _promptTemplateEngine = promptTemplateEngine;
        return this;
    }

    public KernelBuilder WithHttpHandlerFactory(IDelegatingHandlerFactory httpHandlerFactory)
    {
        Verify.NotNull(httpHandlerFactory);
        _httpHandlerFactory = httpHandlerFactory;
        return this;
    }

    [Obsolete("This method is deprecated, use WithHttpHandlerFactory instead")]
    public KernelBuilder WithRetryHandlerFactory(IDelegatingHandlerFactory httpHandlerFactory)
    {
        return WithHttpHandlerFactory(httpHandlerFactory);
    }

    public KernelBuilder WithDefaultAIService<TService>(TService instance) where TService : IAIService
    {
        _aiServices.SetService<TService>(instance);
        return this;
    }

    public KernelBuilder WithDefaultAIService<TService>(Func<ILoggerFactory, TService> factory) where TService : IAIService
    {
        _aiServices.SetService<TService>(() => factory(this._loggerFactory));
        return this;
    }

    public KernelBuilder WithAIService<TService>(
        string? serviceId,
        TService instance,
        bool setAsDefault = false) where TService : IAIService
    {
        _aiServices.SetService<TService>(serviceId, instance, setAsDefault);
        return this;
    }

    public KernelBuilder WithAIService<TService>(
        string? serviceId,
        Func<ILoggerFactory, TService> factory,
        bool setAsDefault = false) where TService : IAIService
    {
        _aiServices.SetService<TService>(serviceId, () => factory(_loggerFactory), setAsDefault);
        return this;
    }

    public KernelBuilder WithAIService<TService>(
        string? serviceId,
        Func<ILoggerFactory, IDelegatingHandlerFactory, TService> factory,
        bool setAsDefault = false) where TService : IAIService
    {
        _aiServices.SetService<TService>(serviceId, () => factory(_loggerFactory, _httpHandlerFactory), setAsDefault);
        return this;
    }

    private IPromptTemplateEngine CreateDefaultPromptTemplateEngine(ILoggerFactory? loggerFactory = null)
    {
        if (!_promptTemplateEngineInitialized)
        {
            _promptTemplateEngineType = GetPromptTemplateEngineType();
            _promptTemplateEngineInitialized = true;
        }

        if (_promptTemplateEngineType is not null)
        {
            var constructor = _promptTemplateEngineType.GetConstructor(new Type[] { typeof(ILoggerFactory) });
            if (constructor is not null)
            {
#pragma warning disable CS8601 // Null logger factory is OK
                return (IPromptTemplateEngine)constructor.Invoke(new object[] { loggerFactory });
#pragma warning restore CS8601
            }
        }

        return new NullPromptTemplateEngine();
    }

    private Type? GetPromptTemplateEngineType()
    {
        try
        {
            var assembly = Assembly.Load("Microsoft.SemanticKernel.TemplateEngine.PromptTemplateEngine");

            return assembly.ExportedTypes.Single(type =>
                type.Name.Equals("PromptTemplateEngine", StringComparison.Ordinal) &&
                type.GetInterface(nameof(IPromptTemplateEngine)) is not null);
        }
        catch (System.Exception ex) when (!ex.IsCriticalException())
        {
            return null;
        }
    }
}

internal class NullPromptTemplateEngine : IPromptTemplateEngine
{
    public Task<string> RenderAsync(string templateText, SKContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(templateText);
    }
}
