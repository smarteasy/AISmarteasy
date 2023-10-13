using AISmarteasy.Core.Util;
using Azure.AI.OpenAI;

namespace AISmarteasy.Core.Connector.OpenAI;

public class OpenAIFunctionParameter
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public bool IsRequired { get; set; } = false;
}

public class OpenAIFunction
{
    public const string NAME_SEPARATOR = "-";

    public string FunctionName { get; set; } = string.Empty;

    public string PluginName { get; set; } = string.Empty;

    public string FullyQualifiedName =>
        PluginName.IsNullOrEmpty() ? FunctionName : string.Join(NAME_SEPARATOR, PluginName, FunctionName);

    public string Description { get; set; } = string.Empty;

    public IList<OpenAIFunctionParameter> Parameters { get; set; } = new List<OpenAIFunctionParameter>();

    public FunctionDefinition ToFunctionDefinition()
    {
        var requiredParams = new List<string>();

        var paramProperties = new Dictionary<string, object>();
        foreach (var param in this.Parameters)
        {
            paramProperties.Add(
                param.Name,
                new
                {
                    type = param.Type,
                    description = param.Description,
                });

            if (param.IsRequired)
            {
                requiredParams.Add(param.Name);
            }
        }
        return new FunctionDefinition
        {
            Name = this.FullyQualifiedName,
            Description = this.Description,
            Parameters = BinaryData.FromObjectAsJson(
            new
            {
                type = "object",
                properties = paramProperties,
                required = requiredParams,
            }),
        };
    }
}
