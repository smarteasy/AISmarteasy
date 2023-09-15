using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace SemanticKernel.Function;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class FunctionsView
{

    public ConcurrentDictionary<string, List<FunctionView>> SemanticFunctions { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);

    public ConcurrentDictionary<string, List<FunctionView>> NativeFunctions { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);

    public FunctionsView AddFunction(FunctionView view)
    {
        if (view.IsSemantic)
        {
            if (!SemanticFunctions.ContainsKey(view.SkillName))
            {
                SemanticFunctions[view.SkillName] = new();
            }

            SemanticFunctions[view.SkillName].Add(view);
        }
        else
        {
            if (!NativeFunctions.ContainsKey(view.SkillName))
            {
                NativeFunctions[view.SkillName] = new();
            }

            NativeFunctions[view.SkillName].Add(view);
        }

        return this;
    }

    public bool IsSemantic(string skillName, string functionName)
    {
        var sf = SemanticFunctions.ContainsKey(skillName)
                 && SemanticFunctions[skillName]
                     .Any(x => string.Equals(x.Name, functionName, StringComparison.OrdinalIgnoreCase));

        var nf = NativeFunctions.ContainsKey(skillName)
                 && NativeFunctions[skillName]
                     .Any(x => string.Equals(x.Name, functionName, StringComparison.OrdinalIgnoreCase));

        if (sf && nf)
        {
            throw new AmbiguousMatchException("There are 2 functions with the same name, one native and one semantic");
        }

        return sf;
    }

    public bool IsNative(string skillName, string functionName)
    {
        var sf = SemanticFunctions.ContainsKey(skillName)
                 && SemanticFunctions[skillName]
                     .Any(x => string.Equals(x.Name, functionName, StringComparison.OrdinalIgnoreCase));

        var nf = NativeFunctions.ContainsKey(skillName)
                 && NativeFunctions[skillName]
                     .Any(x => string.Equals(x.Name, functionName, StringComparison.OrdinalIgnoreCase));

        if (sf && nf)
        {
            throw new AmbiguousMatchException("There are 2 functions with the same name, one native and one semantic");
        }

        return nf;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"Native = {NativeFunctions.Count}, Semantic = {SemanticFunctions.Count}";
}
