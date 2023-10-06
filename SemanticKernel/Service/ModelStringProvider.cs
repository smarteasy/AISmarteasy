namespace SemanticKernel.Service;

public static class ModelStringProvider
{
    public static string ProvideCompletionModel(AIServiceTypeKind serviceType)
    {
        switch (serviceType)
        {
            case AIServiceTypeKind.TextCompletion:
                return "text-davinci-003";
            case AIServiceTypeKind.ChatCompletion:
                return "gpt-4";
            default:
                throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null);
        }
    }

    public static string ProvideEmbeddingModel(MemoryTypeKind memoryType)
    {
        switch (memoryType)
        {
            case MemoryTypeKind.PineCone:
                return "text-embedding-ada-002";
            default:
                throw new ArgumentOutOfRangeException(nameof(memoryType), memoryType, null);
        }
    }
}
