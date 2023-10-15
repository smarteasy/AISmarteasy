using AISmarteasy.Core.Connector.OpenAI.Completion;
using AISmarteasy.Core.Connector.OpenAI.Completion.Chat;

namespace AISmarteasy.Core.Connector.OpenAI.Text.Chat;

public interface IChatResult
{
    ModelResult ModelResult { get; }

    Task<ChatMessageBase> GetChatMessageAsync(CancellationToken cancellationToken = default);
}
