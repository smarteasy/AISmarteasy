using AISmarteasy.Core.Memory;

namespace AISmarteasy.Core.Planning;

public class SemanticMemoryConfig
{
    public HashSet<(string PluginName, string FunctionName)> IncludedFunctions { get; } = new();

    public ISemanticMemory Memory { get; set; } = NullMemory.Instance;

    public int MaxRelevantFunctions { get; set; } = 100;

    public double? RelevancyThreshold { get; set; }
}
