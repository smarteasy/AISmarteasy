using SemanticKernel.Service;

namespace SemanticKernel.Connector.OpenAI.TextCompletion.Chat;

public interface IChatCompletion : IAIService
{
    //ChatHistory CreateNewChat(string? instructions = null);

    //Task<IReadOnlyList<IChatResult>> GetChatCompletionsAsync(
    //    ChatHistory chat,
    //    AIRequestSettings? requestSettings = null,
    //    CancellationToken cancellationToken = default);
}
