using Azure.AI.OpenAI;
using SemanticKernel.Function;

namespace SemanticKernel.Connector.OpenAI.TextCompletion.Chat;

internal sealed class ChatResult : IChatResult, ITextResult
{
    private readonly ChatChoice _choice;

    public ChatResult(ChatCompletions resultData, ChatChoice choice)
    {
        Verify.NotNull(choice);
        _choice = choice;
        ModelResult = new(new ChatModelResult(resultData, choice));
    }

    public ModelResult ModelResult { get; }

    public Task<ChatMessageBase> GetChatMessageAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<ChatMessageBase>(new SKChatMessage(_choice.Message));

    public Task<string> GetCompletionAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_choice.Message.Content);
    }
}
