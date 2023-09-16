using SemanticKernel.Connector.OpenAI.TextCompletion;
using SemanticKernel.Context;

namespace SemanticKernel.Function;

public interface ISKFunction
{
    string Name { get; }

    string SkillName { get; }

    string Description { get; }

    bool IsSemantic { get; }

    CompleteRequestSettings RequestSettings { get; }

    FunctionView Describe();

    Task<SKContext> InvokeAsync(
        SKContext context,
        CompleteRequestSettings? settings = null,
        CancellationToken cancellationToken = default);

    ISKFunction SetDefaultSkillCollection(IReadOnlySkillCollection skills);

    ISKFunction SetAIService(Func<ITextCompletion> serviceFactory);

    ISKFunction SetAIConfiguration(CompleteRequestSettings settings);
}
