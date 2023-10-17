namespace AISmarteasy.Core.Connecting.OpenAI.Text.Chat;

public interface IChatStreamingResult : IChatResult
{
    IAsyncEnumerable<ChatMessageBase> GetStreamingChatMessageAsync(CancellationToken cancellationToken = default);
}
