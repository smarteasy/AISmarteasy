namespace AISmarteasy.Core.PluginFunction;

public sealed record FunctionView(
    string PluginName,
    string Name,
    string Description = "",
    bool IsSemantic = false,
    IList<ParameterView>? Parameters = null)
{
    public IList<ParameterView> Parameters { get; init; } = Parameters ?? Array.Empty<ParameterView>();

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
}