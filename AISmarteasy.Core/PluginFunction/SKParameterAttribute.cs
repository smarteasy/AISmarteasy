namespace AISmarteasy.Core.PluginFunction;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class SKParameterAttribute : Attribute
{
    public SKParameterAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public string Name { get; }

    public string Description { get; }

    public string? DefaultValue { get; set; }
}
