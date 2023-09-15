namespace SemanticKernel.Function;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class SKNameAttribute : Attribute
{
    public SKNameAttribute(string name) => this.Name = name;

    public string Name { get; }
}
