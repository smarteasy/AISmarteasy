using AISmarteasy.Core.Prompt;

namespace AISmarteasy.Core.Function;

public sealed class SemanticFunctionConfig
{
    public string FunctionName { get; }
 
    public string PluginName { get; }

    public PromptTemplateConfig PromptTemplateConfig { get; }

    public IPromptTemplate PromptTemplate { get; }

    public SemanticFunctionConfig(string pluginName, string functionName,
        PromptTemplateConfig config, IPromptTemplate template)
    {
        FunctionName = functionName;
        PluginName = pluginName;
        PromptTemplateConfig = config;
        PromptTemplate = template;
    }
}
