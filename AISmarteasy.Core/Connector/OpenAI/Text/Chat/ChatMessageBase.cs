using AISmarteasy.Core.Connector.OpenAI.Text;

namespace AISmarteasy.Core.Connector.OpenAI.Completion.Chat;

public abstract class ChatMessageBase
{
    public AuthorRole Role { get; set; }

    public string Content { get; set; }

    protected ChatMessageBase(AuthorRole role, string message)
    {
        Role = role;
        Content = message;
    }
}
