namespace SemanticKernel.Service;

public struct AIServiceConfig
{
    public AIServiceKind Service;
    public AIServiceVendorKind Vendor;
    public AIServiceFeatureKind ServiceFeature;
    public string APIKey;
}
