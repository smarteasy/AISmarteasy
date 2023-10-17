using AISmarteasy.Core.Handling;
using AISmarteasy.Core.Service;

namespace AISmarteasy.Core.Connecting.OpenAI.Text.Chat;

public class OpenAIChatHistory : ChatHistory
{
    public OpenAIChatHistory(string? systemMessage = null)
    {
        if (!systemMessage.IsNullOrWhitespace())
        {
            AddSystemMessage(systemMessage);
        }
    }
}
