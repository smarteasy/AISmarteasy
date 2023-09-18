using SemanticKernel.Service;

namespace SemanticKernel.Connector.OpenAI.TextCompletion;

public interface ITextCompletion : IAIService
{
    //TODO - 아래 내용 IAIService로 이동 반영 
    //IAsyncEnumerable<ITextStreamingResult> RunAsyncStreaming(
    //    string text,
    //    CompleteRequestSettings requestSettings,
    //    CancellationToken cancellationToken = default);
}
