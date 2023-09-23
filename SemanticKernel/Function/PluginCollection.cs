﻿using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SemanticKernel.Function;

public class PluginCollection : IPluginCollection
{
    private readonly ILogger _logger;
    internal const string GlobalPlugin = "_GLOBAL_FUNCTIONS_";
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ISKFunction>> _pluginCollection;

    public PluginCollection(ILoggerFactory? loggerFactory = null)
    {
        _logger = loggerFactory is not null ? loggerFactory.CreateLogger(typeof(PluginCollection)) : NullLogger.Instance;
        _pluginCollection = new(StringComparer.OrdinalIgnoreCase);
    }

    public IPluginCollection AddFunction(ISKFunction function)
    {
        Verify.NotNull(function);

        ConcurrentDictionary<string, ISKFunction> plugin = _pluginCollection.GetOrAdd(function.PluginName, static _ => new(StringComparer.OrdinalIgnoreCase));
        plugin[function.Name] = function;

        return this;
    }

    public ISKFunction GetFunction(string pluginName, string functionName)
    {
        if (!TryGetFunction(pluginName, functionName, out ISKFunction? function))
        {
            ThrowFunctionNotAvailable(pluginName, functionName);
        }

        return function!;
    }

    public bool TryGetFunction(string pluginName, string functionName, [NotNullWhen(true)] out ISKFunction? availableFunction)
    {
        Verify.NotNull(pluginName);
        Verify.NotNull(functionName);

        if (_pluginCollection.TryGetValue(pluginName, out ConcurrentDictionary<string, ISKFunction>? skill))
        {
            return skill.TryGetValue(functionName, out availableFunction);
        }

        availableFunction = null;
        return false;
    }

    public FunctionsView GetFunctionsView(bool includeSemantic = true, bool includeNative = true)
    {
        var result = new FunctionsView();

        if (includeSemantic || includeNative)
        {
            foreach (var plugin in this._pluginCollection)
            {
                foreach (KeyValuePair<string, ISKFunction> function in plugin.Value)
                {
                    if (function.Value.IsSemantic ? includeSemantic : includeNative)
                    {
                        result.AddFunction(function.Value.Describe());
                    }
                }
            }
        }

        return result;
    }

    private void ThrowFunctionNotAvailable(string pluginName, string functionName)
    {
        _logger.LogError("Function not available: skill:{0} function:{1}", pluginName, functionName);
        throw new SKException($"Function not available {pluginName}.{functionName}");
    }
}
