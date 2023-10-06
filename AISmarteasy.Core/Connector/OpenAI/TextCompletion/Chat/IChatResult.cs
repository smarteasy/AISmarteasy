namespace AISmarteasy.Core.Connector.OpenAI.TextCompletion.Chat;

public interface IChatResult
{
    ModelResult ModelResult { get; }

    Task<ChatMessageBase> GetChatMessageAsync(CancellationToken cancellationToken = default);
}
