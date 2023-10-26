namespace NativeFunctions;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
public sealed class SKNameAttribute : Attribute
{
    public SKNameAttribute(string name) => Name = name;

    public string Name { get; }
}
