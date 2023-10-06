namespace SemanticKernel.Connector.OpenAI.TextCompletion.Chat;

public class SKChatMessage : ChatMessageBase
{
    public SKChatMessage(Azure.AI.OpenAI.ChatMessage message)
        : base(new AuthorRole(message.Role.ToString()!), message.Content)
    {
    }

    public SKChatMessage(string role, string content)
        : base(new AuthorRole(role), content)
    {
    }
}
