namespace SemanticKernel.Service;

public static class ModelStringProvider
{
    public static string Provide(AIServiceKind aiService)
    {
        switch (aiService)
        {
            case AIServiceKind.OpenAITextCompletion:
                return "text-davinci-003";
            case AIServiceKind.OpenAIChatCompletion:
                return "gpt-4";
            default:
                throw new ArgumentOutOfRangeException(nameof(aiService), aiService, null);
        }
    }
}
