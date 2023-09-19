using SemanticKernel.Connector.OpenAI.TextCompletion;
using SemanticKernel.Context;

namespace SemanticKernel.Function;

public interface ISKFunction
{
    string Name { get; }

    string PluginName { get; }

    string Description { get; }

    bool IsSemantic { get; }

    CompleteRequestSettings RequestSettings { get; }

    FunctionView Describe();

    Task<SKContext> InvokeAsync(
        IKernel kernel,
        CompleteRequestSettings? settings = null,
        CancellationToken cancellationToken = default);

    ISKFunction SetDefaultSkillCollection(IReadOnlyPluginCollection skills);

    ISKFunction SetAIService(Func<ITextCompletion> serviceFactory);

    ISKFunction SetAIConfiguration(CompleteRequestSettings settings);
}
