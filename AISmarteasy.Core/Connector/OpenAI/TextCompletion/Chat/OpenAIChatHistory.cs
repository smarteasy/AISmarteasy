using AISmarteasy.Core.Util;

namespace AISmarteasy.Core.Connector.OpenAI.TextCompletion.Chat;

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
