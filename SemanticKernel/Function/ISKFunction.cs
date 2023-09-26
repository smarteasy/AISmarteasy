using SemanticKernel.Connector.OpenAI.TextCompletion;
using SemanticKernel.Context;

namespace SemanticKernel.Function;

public interface ISKFunction
{
    string Name { get; }

    string PluginName { get; }

    string Description { get; }

    AIRequestSettings RequestSettings { get; }

    FunctionView Describe();

    Task<SKContext> InvokeAsync(
        AIRequestSettings? settings = null,
        CancellationToken cancellationToken = default);

    ISKFunction SetDefaultPluginCollection(IReadOnlyPluginCollection plugins);

    ISKFunction SetAIConfiguration(AIRequestSettings settings);
}
