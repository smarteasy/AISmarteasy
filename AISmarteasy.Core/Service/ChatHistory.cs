using AISmarteasy.Core.Connecting.OpenAI.Text;
using AISmarteasy.Core.Connecting.OpenAI.Text.Chat;

namespace AISmarteasy.Core.Service;

public class ChatHistory : List<ChatMessageBase>
{
    private sealed class ChatMessage : ChatMessageBase
    {
        public ChatMessage(AuthorRole authorRole, string message)
            : base(authorRole, message)
        {
        }
    }

    public List<ChatMessageBase> Messages => this;

    public void AddMessage(AuthorRole authorRole, string message)
    {
        Add(new ChatMessage(authorRole, message));
    }

    public void AddUserMessage(string message)
    {
        AddMessage(AuthorRole.User, message);
    }
    public void AddAssistantMessage(string message)
    {
        AddMessage(AuthorRole.Assistant, message);
        LastContent = message;
    }

    public string LastContent { get; set; } = string.Empty;

    public void AddSystemMessage(string message)
    {
        AddMessage(AuthorRole.System, message);
    }
}
