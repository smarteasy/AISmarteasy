namespace AISmarteasy.Core.Connecting.OpenAI.Text.Chat;

public interface IChatResult
{
    ModelResult ModelResult { get; }

    Task<ChatMessageBase> GetChatMessageAsync(CancellationToken cancellationToken = default);
}
