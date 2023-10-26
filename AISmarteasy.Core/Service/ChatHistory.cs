using AISmarteasy.Core.Connecting.OpenAI.Text;
using AISmarteasy.Core.Connecting.OpenAI.Text.Chat;
using static AISmarteasy.Core.Memory.TextChunker;

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

    public void InsertMessage(int index, AuthorRole authorRole, string content)
    {
        Insert(index, new ChatMessage(authorRole, content));
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

    public int GetTokenCount(string? additionalMessage = null, int skipStart = 0, int skipCount = 0, TokenCounter? tokenCounter = null)
    {
        tokenCounter ??= DefaultTokenCounter;

        var messages = string.Join("\n", this.Where((m, i) => i < skipStart || i >= skipStart + skipCount).Select(m => m.Content));

        if (!string.IsNullOrEmpty(additionalMessage))
        {
            messages = $"{messages}\n{additionalMessage}";
        }

        var tokenCount = tokenCounter(messages);
        return tokenCount;
    }

    private static int DefaultTokenCounter(string input)
    {
        return input.Length / 4;
    }
}
