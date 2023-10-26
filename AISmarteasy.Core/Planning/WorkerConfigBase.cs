using AISmarteasy.Core.Memory;
using AISmarteasy.Core.PluginFunction;

namespace AISmarteasy.Core.Planning;

public abstract class WorkerConfigBase
{
    public Func<string>? GetPromptTemplate { get; set; } = null;

    public HashSet<string> ExcludedPlugins { get; } = new();


    public HashSet<string> ExcludedFunctions { get; } = new();


    public HashSet<(string, string)> IncludedFunctions { get; } = new();

    public ISemanticMemory Memory { get; set; } = null!;

    public int MaxRelevantFunctions { get; set; } = 100;

    public double? RelevancyThreshold { get; set; }

    public Func<WorkerConfigBase, string?, CancellationToken, Task<IOrderedEnumerable<FunctionView>>>? GetAvailableFunctionsAsync { get; set; }

    public Func<string, string, PluginFunction.Function?>? GetFunctionCallback { get; set; }

    public int MaxTokens { get; set; } = 1024;
}
