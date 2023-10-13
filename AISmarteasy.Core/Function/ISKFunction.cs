using AISmarteasy.Core.Connector;
using AISmarteasy.Core.Prompt;

namespace AISmarteasy.Core.Function;

public interface ISKFunction
{
    string Name { get; }

    string PluginName { get; }

    string Description { get; }

    AIRequestSettings RequestSettings { get; }

    FunctionView Describe();

    Task InvokeAsync(AIRequestSettings settings, CancellationToken cancellationToken = default);

    ISKFunction SetDefaultPluginCollection(IPlugin plugins);

    ISKFunction SetAIConfiguration(AIRequestSettings settings);

    IPromptTemplate PromptTemplate { get; }
}
