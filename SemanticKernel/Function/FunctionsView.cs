using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace SemanticKernel.Function;

public sealed class FunctionsView
{
    public ConcurrentDictionary<string, List<FunctionView>> FunctionViews { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);

    public void AddFunction(FunctionView view)
    {
        if (!FunctionViews.ContainsKey(view.PluginName))
        {
            FunctionViews[view.PluginName] = new();
        }

        FunctionViews[view.PluginName].Add(view);
    }
}
