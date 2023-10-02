namespace SemanticKernel.Service;

public struct AIServiceConfig
{
    public AIServiceTypeKind ServiceType;
    public AIServiceVendorKind Vendor = AIServiceVendorKind.OpenAI;
    public AIServiceFeatureKind ServiceFeature = AIServiceFeatureKind.Creativity;
    public MemoryTypeKind MemoryType = MemoryTypeKind.None;
    public string APIKey;
    public string? MemoryEnvironment;
    public string? MemoryApiKey;

    public AIServiceConfig(AIServiceTypeKind serviceType, string apiKey, string? memoryApiKey = default, string? memoryEnvironment = default)
    {
        ServiceType = serviceType;
        APIKey = apiKey;
        MemoryEnvironment = memoryEnvironment;
        MemoryApiKey = memoryApiKey;
    }
}
