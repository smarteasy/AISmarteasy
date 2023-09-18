namespace SemanticKernel.Service;

public static class ModelStringProvider
{
    public static string Provide(AIServiceTypeKind aiService)
    {
        switch (aiService)
        {
            case AIServiceTypeKind.OpenAITextCompletion:
                return "text-davinci-003";
            case AIServiceTypeKind.OpenAIChatCompletion:
                return "gpt-4";
            default:
                throw new ArgumentOutOfRangeException(nameof(aiService), aiService, null);
        }
    }
}
