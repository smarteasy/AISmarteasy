using System.Diagnostics;

namespace SemanticKernel;

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
        this.Name = name;
        this.SkillName = skillName;
        this.Description = description;
        this.Parameters = parameters;
        this.IsSemantic = isSemantic;
        this.IsAsynchronous = isAsynchronous;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"{this.Name} ({this.Description})";
}
