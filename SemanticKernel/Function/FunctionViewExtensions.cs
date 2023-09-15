namespace SemanticKernel.Function;

internal static class FunctionViewExtensions
{
    internal static string ToManualString(this FunctionView function)
    {
        var inputs = string.Join("\n", function.Parameters.Select(parameter =>
        {
            var defaultValueString = string.IsNullOrEmpty(parameter.DefaultValue) ? string.Empty : $" (default value: {parameter.DefaultValue})";
            return $"  - {parameter.Name}: {parameter.Description}{defaultValueString}";
        }));

        return $@"{function.ToFullyQualifiedName()}:
  description: {function.Description}
  inputs:
  {inputs}";
    }

    internal static string ToFullyQualifiedName(this FunctionView function)
    {
        return $"{function.SkillName}.{function.Name}";
    }

    internal static string ToEmbeddingString(this FunctionView function)
    {
        var inputs = string.Join("\n", function.Parameters.Select(p => $"    - {p.Name}: {p.Description}"));
        return $"{function.Name}:\n  description: {function.Description}\n  inputs:\n{inputs}";
    }
}
