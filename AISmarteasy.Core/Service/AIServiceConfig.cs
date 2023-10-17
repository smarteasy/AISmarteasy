using AISmarteasy.Core.Handling;
using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Service;

public struct AIServiceConfig
{
    public ILoggerFactory? LoggerFactory;
    public AIServiceVendorKind Vendor = AIServiceVendorKind.OpenAI;
    public AIServiceFeatureKind ServiceFeature = AIServiceFeatureKind.Normal;
    public IDelegatingHandlerFactory? HttpHandlerFactory;
    public AIServiceTypeKind ServiceType;
    public ImageGenerationTypeKind ImageGenerationType;
    public MemoryTypeKind MemoryType;
    public string APIKey;
    public string? ModelId;
    public string? EndPoint;
    public string? ImageGenerationApiKey;
    public string? MemoryEnvironment;
    public string? MemoryApiKey;

    public AIServiceConfig(AIServiceTypeKind serviceType, string apiKey,
        string? modelId, string? endPoint, 
        ImageGenerationTypeKind imageGenerationType = default, MemoryTypeKind memoryType = default, 
        string? imageGenerationApiKey = default,
        string? memoryApiKey = default, string? memoryEnvironment = default)
    {
        ServiceType = serviceType;
        APIKey = apiKey;
        ModelId = modelId;
        EndPoint = endPoint;
        MemoryType = memoryType;
        ImageGenerationType = imageGenerationType;

        ImageGenerationApiKey = imageGenerationApiKey;
        MemoryEnvironment = memoryEnvironment;
        MemoryApiKey = memoryApiKey;
    }

    public AIServiceConfig(AIServiceTypeKind serviceType, string apiKey)
    {
        ServiceType = serviceType;
        APIKey = apiKey;
    }

    public AIServiceConfig(AIServiceTypeKind serviceType, string apiKey, ImageGenerationTypeKind imageGenerationType, 
        MemoryTypeKind memoryType, string memoryApiKey, string memoryEnvironment)
    {
        ServiceType = serviceType;
        APIKey = apiKey;
        ImageGenerationType = imageGenerationType;
        MemoryType = memoryType;
        MemoryEnvironment = memoryEnvironment;
        MemoryApiKey = memoryApiKey;
    }

    public AIServiceConfig(AIServiceTypeKind serviceType, string apiKey, ImageGenerationTypeKind imageGenerationType)
    {
        ServiceType = serviceType;
        APIKey = apiKey;
        ImageGenerationType = imageGenerationType;
    }
}
