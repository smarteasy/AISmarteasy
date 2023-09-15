using System.Diagnostics;

namespace SemanticKernel.Function;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class FunctionView
{
    public string Name { get; set; } = string.Empty;

    public string SkillName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsSemantic { get; set; }

    public bool IsAsynchronous { get; set; }

    public IList<ParameterView> Parameters { get; set; } = new List<ParameterView>();

    public FunctionView()
    {
    }

    public FunctionView(
        string name,
        string skillName,
        string description,
        IList<ParameterView> parameters,
        bool isSemantic,
        bool isAsynchronous = true)
    {
        Name = name;
        SkillName = skillName;
        Description = description;
        Parameters = parameters;
        IsSemantic = isSemantic;
        IsAsynchronous = isAsynchronous;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"{Name} ({Description})";
}
