#pragma warning disable CA1720 // Identifier contains type name

namespace SemanticKernel;

public class ParameterViewType : IEquatable<ParameterViewType>
{
    private readonly string _name;

    public static readonly ParameterViewType String = new("string");

    public static readonly ParameterViewType Number = new("number");

    public static readonly ParameterViewType Object = new("object");

    public static readonly ParameterViewType Array = new("array");

    public static readonly ParameterViewType Boolean = new("boolean");

    public ParameterViewType(string name)
    {
        Verify.NotNullOrWhiteSpace(name, nameof(name));

        this._name = name;
    }

    public string Name => this._name;

    public override string ToString() => this._name;

    public bool Equals(ParameterViewType other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object obj)
    {
        return obj is ParameterViewType other && this.Equals(other);
    }

    public override int GetHashCode()
    {
        return this.Name.GetHashCode();
    }
}
