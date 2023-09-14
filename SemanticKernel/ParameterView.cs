using System.Diagnostics;

namespace SemanticKernel;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class ParameterView
{
    private string _name = string.Empty;

    public string Name
    {
        get => this._name;
        set
        {
            Verify.ValidFunctionParamName(value);
            this._name = value;
        }
    }

    public string? Description { get; set; }

    public string? DefaultValue { get; set; }

    public ParameterViewType? Type { get; set; }

    public ParameterView()
    {
    }

    public ParameterView(
        string name,
        string? description = null,
        string? defaultValue = null,
        ParameterViewType? type = null)
    {
        this.Name = name;
        this.Description = description;
        this.DefaultValue = defaultValue;
        this.Type = type;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => string.IsNullOrEmpty(this.Description)
        ? this.Name
        : $"{this.Name} ({this.Description})";
}
