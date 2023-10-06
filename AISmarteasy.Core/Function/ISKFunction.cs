﻿using AISmarteasy.Core.Context;

namespace AISmarteasy.Core.Function;

public interface ISKFunction
{
    string Name { get; }

    string PluginName { get; }

    string Description { get; }

    AIRequestSettings RequestSettings { get; }

    FunctionView Describe();

    Task<SKContext> InvokeAsync(SKContext context,
        AIRequestSettings? settings = null,
        CancellationToken cancellationToken = default);

    ISKFunction SetDefaultPluginCollection(IPlugin plugins);

    ISKFunction SetAIConfiguration(AIRequestSettings settings);
}
