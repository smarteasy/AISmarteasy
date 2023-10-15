using AISmarteasy.Core.Handler;
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
    public string? ImageGenerationApiKey;
    public string? MemoryEnvironment;
    public string? MemoryApiKey;

    public AIServiceConfig(AIServiceTypeKind serviceType, string apiKey,
        ImageGenerationTypeKind imageGenerationType = default, MemoryTypeKind memoryType = default, 
        string? imageGenerationApiKey = default,
        string? memoryApiKey = default, string? memoryEnvironment = default)
    {
        ServiceType = serviceType;
        APIKey = apiKey;
        MemoryType = memoryType;
        ImageGenerationType = imageGenerationType;

        ImageGenerationApiKey = imageGenerationApiKey;
        MemoryEnvironment = memoryEnvironment;
        MemoryApiKey = memoryApiKey;
    }
}
