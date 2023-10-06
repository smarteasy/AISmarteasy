namespace SemanticKernel.Service;

public struct AIServiceConfig
{
    public AIServiceTypeKind ServiceType;
    public AIServiceVendorKind Vendor = AIServiceVendorKind.OpenAI;
    public AIServiceFeatureKind ServiceFeature = AIServiceFeatureKind.Creativity;
    public MemoryTypeKind MemoryType;
    public string APIKey;
    public string? MemoryEnvironment;
    public string? MemoryApiKey;

    public AIServiceConfig(AIServiceTypeKind serviceType, string apiKey, MemoryTypeKind memoryType = default, 
        string? memoryApiKey = default, string? memoryEnvironment = default)
    {
        ServiceType = serviceType;
        APIKey = apiKey;
        MemoryType = memoryType;
        MemoryEnvironment = memoryEnvironment;
        MemoryApiKey = memoryApiKey;
    }
}
