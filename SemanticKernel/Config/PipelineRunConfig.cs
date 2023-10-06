using Newtonsoft.Json.Linq;

namespace SemanticKernel.Config;

public readonly struct PipelineRunConfig
{
    public List<PluginFunctionName> PluginFunctionNames { get; } = new List<PluginFunctionName>();

    private const string INPUT_PARAMETER_KEY = "input";

    public PipelineRunConfig()
    {
    }

    public void AddPluginFunctionName(string pluginName, string functionName)
    {
        PluginFunctionNames.Add(new PluginFunctionName(pluginName, functionName));
    }
}

public readonly struct PluginFunctionName
{
    public string PluginName { get; }
    public string FunctionName { get; }

    public PluginFunctionName(string pluginName, string functionName)
    {
        PluginName = pluginName;
        FunctionName = functionName;
    }


}
