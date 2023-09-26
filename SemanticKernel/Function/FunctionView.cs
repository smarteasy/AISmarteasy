using System.Diagnostics;

namespace SemanticKernel.Function;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class FunctionView
{
    public string Name { get; set; } = string.Empty;

    public string PluginName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsAsynchronous { get; set; }

    public IList<ParameterView> Parameters { get; set; } = new List<ParameterView>();

    public FunctionView()
    {
    }

    public FunctionView(
        string name,
        string pluginName,
        string description,
        IList<ParameterView> parameters,
        bool isAsynchronous = true)
    {
        Name = name;
        PluginName = pluginName;
        Description = description;
        Parameters = parameters;
        IsAsynchronous = isAsynchronous;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"{Name} ({Description})";

    public string ToManualString()
    {
        var inputs = string.Join("\n", Parameters.Select(parameter =>
        {
            var defaultValueString = string.IsNullOrEmpty(parameter.DefaultValue) ? string.Empty : $" (default value: {parameter.DefaultValue})";
            return $"  - {parameter.Name}: {parameter.Description}{defaultValueString}";
        }));

        return $@"{ToFullyQualifiedName()}:
  description: {Description}
  inputs:
  {inputs}";
    }
    public string ToFullyQualifiedName()
    {
        return $"{PluginName}.{Name}";
    }
    public string ToEmbeddingString()
    {
        var inputs = string.Join("\n", Parameters.Select(p => $"    - {p.Name}: {p.Description}"));
        return $"{Name}:\n  description: {Description}\n  inputs:\n{inputs}";
    }
}
