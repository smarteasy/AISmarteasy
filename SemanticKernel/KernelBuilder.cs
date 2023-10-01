using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SemanticKernel.Connector.Memory;
using SemanticKernel.Connector.Memory.Pinecone;
using SemanticKernel.Connector.OpenAI;
using SemanticKernel.Connector.OpenAI.TextCompletion;
using SemanticKernel.Connector.OpenAI.TextCompletion.Chat;
using SemanticKernel.Embedding;
using SemanticKernel.Function;
using SemanticKernel.Handler;
using SemanticKernel.Memory;
using SemanticKernel.Prompt;
using SemanticKernel.Service;

namespace SemanticKernel;

public sealed class KernelBuilder
{
    private readonly IPromptTemplate _promptTemplateEngine;
    private ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;
    private IDelegatingHandlerFactory _httpHandlerFactory = NullHttpHandlerFactory.Instance;

    private Func<IMemoryStore>? _memoryStorageFactory;
    private IAIService? _service;

    public KernelBuilder()
    {
        _promptTemplateEngine = new PromptTemplate(_loggerFactory);
    }

    public Kernel Build(AIServiceConfig config)
    {
        var model = ModelStringProvider.Provide(config.Service);

        switch (config.Service)
        {
            case AIServiceKind.TextCompletion:
                WithOpenAITextCompletionService(model, config.APIKey);
                break;
            case AIServiceKind.ChatCompletion:
                WithOpenAIChatCompletionService(model, config.APIKey);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(config.Service), config.Service, null);
        }

        var kernel = new Kernel(_service!, _httpHandlerFactory, _loggerFactory);

        KernelProvider.Kernel = kernel;

        return kernel;
    }

    public Kernel Build()
    {
        var kernel = new Kernel(_service!, _httpHandlerFactory, _loggerFactory);

        if (_memoryStorageFactory != null)
        {
            kernel.UseMemory((_service as ITextEmbeddingGeneration)!, _memoryStorageFactory.Invoke());
        }

        KernelProvider.Kernel = kernel;

        return kernel;
    }

    public KernelBuilder WithOpenAIService(AIServiceKind aiServiceType, string apiKey)
    {
        return WithOpenAIService(aiServiceType, apiKey, string.Empty, string.Empty);
    }

    public KernelBuilder WithOpenAIService(AIServiceKind aiServiceType, string apiKey, string memoryEnvironment,  string memoryApiKey)
    {
        var model = ModelStringProvider.Provide(aiServiceType);

        switch (aiServiceType)
        {
            case AIServiceKind.Embedding:
                WithOpenAIEmbeddingService(model, apiKey)
                    .WithMemoryStorage(new PineconeMemoryStore(memoryEnvironment, memoryApiKey));
                break;
            case AIServiceKind.TextCompletion:
                WithOpenAITextCompletionService(model, apiKey);
                break;
            case AIServiceKind.ChatCompletion:
               WithOpenAIChatCompletionService(model, apiKey);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(aiServiceType), aiServiceType, null);
        }

        return this;
    }

    public KernelBuilder WithMemoryStorage(IMemoryStore storage)
    {
        Verify.NotNull(storage);
        _memoryStorageFactory = () => storage;
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

    private KernelBuilder WithOpenAIChatCompletionService(string model, string apiKey)
    {
        _service = new OpenAIChatCompletion(model, apiKey);
        return this;
    }

    private KernelBuilder WithOpenAIEmbeddingService(string model, string apiKey)
    {
        _service = new OpenAITextEmbeddingGeneration(model, apiKey);
        return this;
    }
    
    public KernelBuilder WithLoggerFactory(ILoggerFactory loggerFactory)
    {
        Verify.NotNull(loggerFactory);
        _loggerFactory = loggerFactory;
        return this;
    }
}
