﻿using AISmarteasy.Core.Connecting.OpenAI;
using AISmarteasy.Core.Connecting.Pinecone;
using AISmarteasy.Core.Handling;
using AISmarteasy.Core.Memory;
using AISmarteasy.Core.Service;

namespace AISmarteasy.Core;

public static class KernelBuilder
{
    public static void Build(AIServiceConfig config)
    {
        var modelId = ModelStringProvider.ProvideCompletionModel(config.ServiceType);
        Func<IMemoryStore>? memoryStorageFactory = null; 
        ITextCompletion completionService;
        IEmbeddingGeneration? embeddingService = null;

        switch (config.ServiceType)
        {
            case AIServiceTypeKind.TextCompletion:
                completionService = new OpenAITextCompletion(modelId, config.APIKey, config.ServiceType);
                break;
            case AIServiceTypeKind.ChatCompletion:
            case AIServiceTypeKind.ChatCompletionWithGpt35:
                completionService = new OpenAIChatCompletion(modelId, config.APIKey, config.ServiceType);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(config.ServiceType), config.ServiceType, null);
        }

        if (config.MemoryType == MemoryTypeKind.PineCone)
        {
            var embeddingModel = ModelStringProvider.ProvideEmbeddingModel(config.MemoryType);
            embeddingService = new OpenAIEmbeddingGeneration(embeddingModel, config.APIKey);
            memoryStorageFactory = () => new PineconeMemoryStore(config.MemoryEnvironment!, config.MemoryApiKey!);
        }

        config.LoggerFactory ??= ConsoleLogger.LoggerFactory;
        config.HttpHandlerFactory ??= NullHttpHandlerFactory.Instance;

        var kernel = new Kernel(completionService, config.HttpHandlerFactory, config.LoggerFactory);

        if (memoryStorageFactory != null)
        {
            kernel.UseMemory(embeddingService!, memoryStorageFactory.Invoke());
        }

        if (config.ImageGenerationType == ImageGenerationTypeKind.DallE)
        {
            IImageGeneration imageGenerationService = new OpenAIImageGeneration(config.APIKey);
            kernel.ImageGenerationService = imageGenerationService;
        }

        KernelProvider.Initialize(kernel);
        kernel.LoadPlugin();
        kernel.BuildFunctionViews();
    }
}
