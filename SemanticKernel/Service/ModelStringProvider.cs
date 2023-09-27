namespace SemanticKernel.Service;

public static class ModelStringProvider
{
    public static string Provide(AIServiceKind aiService)
    {
        switch (aiService)
        {
            case AIServiceKind.TextCompletion:
                return "text-davinci-003";
            case AIServiceKind.ChatCompletion:
                return "gpt-4";
            default:
                throw new ArgumentOutOfRangeException(nameof(aiService), aiService, null);
        }
    }
}
