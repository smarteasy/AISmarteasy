namespace SemanticKernel.Connector.OpenAI.TextCompletion.Chat;

public class ChatHistory : List<ChatMessageBase>
{
    private sealed class ChatMessage : ChatMessageBase
    {
        public ChatMessage(AuthorRole authorRole, string content) : base(authorRole, content)
        {
        }
    }

    public List<ChatMessageBase> Messages => this;

    public void AddMessage(AuthorRole authorRole, string content)
    {
        Add(new ChatMessage(authorRole, content));
    }

    public void InsertMessage(int index, AuthorRole authorRole, string content)
    {
        Insert(index, new ChatMessage(authorRole, content));
    }

    public void AddUserMessage(string content)
    {
        AddMessage(AuthorRole.User, content);
    }
    public void AddAssistantMessage(string content)
    {
        AddMessage(AuthorRole.Assistant, content);
    }

    public void AddSystemMessage(string content)
    {
        AddMessage(AuthorRole.System, content);
    }
}
