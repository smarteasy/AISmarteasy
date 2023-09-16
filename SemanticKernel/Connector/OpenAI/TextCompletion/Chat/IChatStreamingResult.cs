﻿namespace SemanticKernel.Connector.OpenAI.TextCompletion.Chat;

public interface IChatStreamingResult : IChatResult
{
    IAsyncEnumerable<ChatMessageBase> GetStreamingChatMessageAsync(CancellationToken cancellationToken = default);
}
