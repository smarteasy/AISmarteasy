using AISmarteasy.Core.Function;
using AISmarteasy.Core.Memory;

namespace AISmarteasy.Core.Planner;

public abstract class PlannerConfigBase
{
    public Func<string>? GetPromptTemplate { get; set; } = null;

    public HashSet<string> ExcludedPlugins { get; } = new();


    public HashSet<string> ExcludedFunctions { get; } = new();


    public HashSet<(string, string)> IncludedFunctions { get; } = new();

    public ISemanticMemory Memory { get; set; } = null!;

    public int MaxRelevantFunctions { get; set; } = 100;

    public double? RelevancyThreshold { get; set; }

    public Func<PlannerConfigBase, string?, CancellationToken, Task<IOrderedEnumerable<FunctionView>>>? GetAvailableFunctionsAsync { get; set; }

    public Func<string, string, ISKFunction?>? GetFunctionCallback { get; set; }
}
