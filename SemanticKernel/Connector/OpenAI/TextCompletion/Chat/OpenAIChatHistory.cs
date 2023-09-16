using SemanticKernel.Util;

namespace SemanticKernel.Connector.OpenAI.TextCompletion.Chat;

public class OpenAIChatHistory : ChatHistory
{
    public OpenAIChatHistory(string? assistantInstructions = null)
    {
        if (!assistantInstructions.IsNullOrWhitespace())
        {
            AddSystemMessage(assistantInstructions);
        }
    }
}
