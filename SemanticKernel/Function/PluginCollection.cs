using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SemanticKernel.Function;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
[DebuggerTypeProxy(typeof(ReadOnlyPluginCollectionTypeProxy))]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class PluginCollection : IPluginCollection
{
    internal const string GlobalPlugin = "_GLOBAL_FUNCTIONS_";

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

    public ISKFunction GetFunction(string functionName) =>
        this.GetFunction(GlobalPlugin, functionName);

    public ISKFunction GetFunction(string pluginName, string functionName)
    {
        if (!this.TryGetFunction(pluginName, functionName, out ISKFunction? functionInstance))
        {
            this.ThrowFunctionNotAvailable(pluginName, functionName);
        }

        return functionInstance;
    }

    public bool TryGetFunction(string functionName, [NotNullWhen(true)] out ISKFunction? availableFunction) =>
        this.TryGetFunction(GlobalPlugin, functionName, out availableFunction);

    public bool TryGetFunction(string pluginName, string functionName, [NotNullWhen(true)] out ISKFunction? availableFunction)
    {
        Verify.NotNull(pluginName);
        Verify.NotNull(functionName);

        if (this._pluginCollection.TryGetValue(pluginName, out ConcurrentDictionary<string, ISKFunction>? skill))
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
                foreach (KeyValuePair<string, ISKFunction> f in plugin.Value)
                {
                    if (f.Value.IsSemantic ? includeSemantic : includeNative)
                    {
                        result.AddFunction(f.Value.Describe());
                    }
                }
            }
        }

        return result;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal string DebuggerDisplay => $"Count = {_pluginCollection.Count}";

    [DoesNotReturn]
    private void ThrowFunctionNotAvailable(string pluginName, string functionName)
    {
        this._logger.LogError("Function not available: skill:{0} function:{1}", pluginName, functionName);
        throw new SKException($"Function not available {pluginName}.{functionName}");
    }

    private readonly ILogger _logger;

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ISKFunction>> _pluginCollection;
}
