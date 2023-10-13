namespace AISmarteasy.Core.Service;

public static class ModelStringProvider
{
    public static string ProvideCompletionModel(AIServiceTypeKind serviceType)
    {
        return serviceType switch
        {
            AIServiceTypeKind.TextCompletion => "text-davinci-003",
            AIServiceTypeKind.ChatCompletion => "gpt-4",
            AIServiceTypeKind.ChatCompletionWithGpt35 => "gpt-3.5-turbo",
            _ => throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null)
        };
    }

    public static string ProvideEmbeddingModel(MemoryTypeKind memoryType)
    {
        return memoryType switch
        {
            MemoryTypeKind.PineCone => "text-embedding-ada-002",
            _ => throw new ArgumentOutOfRangeException(nameof(memoryType), memoryType, null)
        };
    }
}
