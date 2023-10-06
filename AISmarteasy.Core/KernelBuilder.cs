using AISmarteasy.Core.Connector.OpenAI;
using AISmarteasy.Core.Connector.OpenAI.TextCompletion;
using AISmarteasy.Core.Connector.OpenAI.TextCompletion.Chat;
using AISmarteasy.Core.Function;
using AISmarteasy.Core.Handler;
using AISmarteasy.Core.Memory;
using AISmarteasy.Core.Prompt;
using AISmarteasy.Core.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISmarteasy.Core;

public sealed class KernelBuilder
{
    private readonly IPromptTemplate _promptTemplateEngine;
    private ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;
    private IDelegatingHandlerFactory _httpHandlerFactory = NullHttpHandlerFactory.Instance;

    private Func<IMemoryStore>? _memoryStorageFactory;
    private IAIService? _completionService;
    private IAIService? _embeddingService;

    public KernelBuilder()
    {
        _promptTemplateEngine = new PromptTemplate(_loggerFactory);
    }

    public Kernel Build(AIServiceConfig config)
    {
        var model = ModelStringProvider.ProvideCompletionModel(config.ServiceType);

        switch (config.ServiceType)
        {
            case AIServiceTypeKind.TextCompletion:
                WithOpenAITextCompletionService(model, config.APIKey);
                break;
            case AIServiceTypeKind.ChatCompletion:
                WithOpenAIChatCompletionService(model, config.APIKey);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(config.ServiceType), config.ServiceType, null);
        }

        if (config.MemoryType == MemoryTypeKind.PineCone)
        {
            var embeddingModel = ModelStringProvider.ProvideEmbeddingModel(config.MemoryType);
            WithOpenAIEmbeddingService(embeddingModel, config.APIKey);
            WithMemoryStorage(new PineconeMemoryStore(config.MemoryEnvironment!, config.MemoryApiKey!));
        }

        var kernel = new Kernel(_completionService!, _httpHandlerFactory, _loggerFactory);

        if (_memoryStorageFactory != null)
        {
            kernel.UseMemory((_embeddingService as IAIService)!, _memoryStorageFactory.Invoke());
        }

        KernelProvider.Kernel = kernel;

        return kernel;
    }

    public KernelBuilder WithMemoryStorage(IMemoryStore storage)
    {
        Verify.NotNull(storage);
        _memoryStorageFactory = () => storage;
        return this;
    }
    

    private KernelBuilder WithOpenAITextCompletionService(string model, string apiKey)
    {
        _completionService = new OpenAITextCompletion(model, apiKey);
        return this;
    }

    private KernelBuilder WithOpenAIChatCompletionService(string model, string apiKey)
    {
        _completionService = new OpenAIChatCompletion(model, apiKey);
        return this;
    }

    private KernelBuilder WithOpenAIEmbeddingService(string model, string apiKey)
    {
        _embeddingService = new OpenAITextEmbeddingGeneration(model, apiKey);
        return this;
    }






    public KernelBuilder WithHttpHandlerFactory(IDelegatingHandlerFactory httpHandlerFactory)
    {
        Verify.NotNull(httpHandlerFactory);
        _httpHandlerFactory = httpHandlerFactory;
        return this;
    }

    public KernelBuilder WithLoggerFactory(ILoggerFactory loggerFactory)
    {
        Verify.NotNull(loggerFactory);
        _loggerFactory = loggerFactory;
        return this;
    }
}
