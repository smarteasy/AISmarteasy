using AISmarteasy.Core.Connector.OpenAI.Completion.Chat;

namespace AISmarteasy.Core.Connector.OpenAI.Text.Chat;

public interface IChatStreamingResult : IChatResult
{
    IAsyncEnumerable<ChatMessageBase> GetStreamingChatMessageAsync(CancellationToken cancellationToken = default);
}
