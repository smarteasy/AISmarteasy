using System.Runtime.CompilerServices;
using Azure.AI.OpenAI;
using SemanticKernel.Function;
using SemanticKernel.Memory;

namespace SemanticKernel.Connector.OpenAI.TextCompletion.Chat;

internal sealed class ChatStreamingResult : IChatStreamingResult, ITextStreamingResult
{
    private readonly StreamingChatChoice _choice;

    public ChatStreamingResult(StreamingChatCompletions resultData, StreamingChatChoice choice)
    {
        Verify.NotNull(choice);
        ModelResult = new ModelResult(resultData);
        _choice = choice;
    }

    public ModelResult ModelResult { get; }

    public async Task<ChatMessageBase> GetChatMessageAsync(CancellationToken cancellationToken = default)
    {
        var chatMessage = await _choice.GetMessageStreaming(cancellationToken)
                                                .LastOrDefaultAsync(cancellationToken)
                                                .ConfigureAwait(false);

        if (chatMessage is null)
        {
            throw new SKException("Unable to get chat message from stream");
        }

        return new SKChatMessage(chatMessage);
    }

    public async IAsyncEnumerable<ChatMessageBase> GetStreamingChatMessageAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var message in _choice.GetMessageStreaming(cancellationToken))
        {
            yield return new SKChatMessage(message);
        }
    }

    public async Task<string> GetCompletionAsync(CancellationToken cancellationToken = default)
    {
        return (await GetChatMessageAsync(cancellationToken).ConfigureAwait(false)).Content;
    }

    public async IAsyncEnumerable<string> GetCompletionStreamingAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var result in GetStreamingChatMessageAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return result.Content;
        }
    }
}
