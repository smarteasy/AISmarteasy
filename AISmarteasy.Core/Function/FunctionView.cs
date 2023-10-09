namespace AISmarteasy.Core.Function;

public sealed record FunctionView(
    string Name,
    string PluginName,
    string Description = "",
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