namespace SemanticKernel.Connector.OpenAI.TextCompletion.Chat;

public abstract class ChatMessageBase
{
    public AuthorRole Role { get; set; }

    public string Content { get; set; }

    protected ChatMessageBase(AuthorRole role, string content)
    {
        Role = role;
        Content = content;
    }
}
