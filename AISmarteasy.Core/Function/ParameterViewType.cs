#pragma warning disable CA1720 // Identifier contains type name


namespace AISmarteasy.Core.Function;

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
        Verify.NotNullOrWhitespace(name, nameof(name));

        _name = name;
    }

    public string Name => _name;

    public override string ToString() => _name;

    public bool Equals(ParameterViewType? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is ParameterViewType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}
