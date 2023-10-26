using AISmarteasy.Core.PluginFunction;

namespace AISmarteasy.Core.Planning;

public abstract class PlannerConfigBase
{
    public Func<string>? GetPromptTemplate { get; set; }

    public HashSet<string> ExcludedPlugins { get; } = new();

    public HashSet<string> ExcludedFunctions { get; } = new();


    public SemanticMemoryConfig SemanticMemoryConfig { get; set; } = new();

    public Func<PlannerConfigBase, string?, CancellationToken, Task<IOrderedEnumerable<FunctionView>>>? GetAvailableFunctionsAsync { get; set; }

    public Func<string, string, Function?>? GetFunctionCallback { get; set; }

    public int MaxTokens { get; set; }
}
