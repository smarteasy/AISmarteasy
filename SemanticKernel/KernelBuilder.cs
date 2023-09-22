using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SemanticKernel.Connector.OpenAI;
using SemanticKernel.Connector.OpenAI.TextCompletion;
using SemanticKernel.Context;
using SemanticKernel.Function;
using SemanticKernel.Handler;
using SemanticKernel.Memory;
using SemanticKernel.Prompt;
using SemanticKernel.Service;

namespace SemanticKernel;

public sealed class KernelBuilder
{
    private readonly IPromptTemplate _promptTemplateEngine;
    private Func<ISemanticTextMemory> _memoryFactory = () => NullMemory.Instance;
    private ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;
    private IDelegatingHandlerFactory _httpHandlerFactory = NullHttpHandlerFactory.Instance;

    private Func<IMemoryStore>? _memoryStorageFactory;
    private IAIService? _service;

    public KernelBuilder()
    {
        _promptTemplateEngine = new PromptTemplate(_loggerFactory);
    }

    public IKernel Build()
    {
        var kernel = new Kernel(_service!, _memoryFactory.Invoke(), _httpHandlerFactory, _loggerFactory);

        if (_memoryStorageFactory != null)
        {
            kernel.UseMemory(_memoryStorageFactory.Invoke());
        }

        KernelProvider.Kernel = kernel;

        return kernel;
    }

    public KernelBuilder WithCompletionService(AIServiceTypeKind aiServiceType, string apiKey)
    {
        var model = ModelStringProvider.Provide(aiServiceType);

        var kernelBuilder = new KernelBuilder();

        switch (aiServiceType)
        {
            case AIServiceTypeKind.OpenAITextCompletion:
                kernelBuilder
                    .WithOpenAITextCompletionService(model, apiKey);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(aiServiceType), aiServiceType, null);
        }

        return kernelBuilder;
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

    public KernelBuilder WithMemoryStorage<TStore>(Func<ILoggerFactory, IDelegatingHandlerFactory, TStore> factory)
        where TStore : IMemoryStore
    {
        Verify.NotNull(factory);
        this._memoryStorageFactory = () => factory(this._loggerFactory, this._httpHandlerFactory);
        return this;
    }

    public KernelBuilder WithHttpHandlerFactory(IDelegatingHandlerFactory httpHandlerFactory)
    {
        Verify.NotNull(httpHandlerFactory);
        _httpHandlerFactory = httpHandlerFactory;
        return this;
    }


    private KernelBuilder WithOpenAITextCompletionService(string model, string apiKey)
    {
        _service = new OpenAITextCompletion(model, apiKey);
        return this;
    }

    public KernelBuilder WithLoggerFactory(ILoggerFactory loggerFactory)
    {
        Verify.NotNull(loggerFactory);
        _loggerFactory = loggerFactory;
        return this;
    }
}
