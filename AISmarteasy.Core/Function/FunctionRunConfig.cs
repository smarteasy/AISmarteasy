namespace AISmarteasy.Core.Function;

public readonly struct FunctionRunConfig
{
    public string PluginName { get; }

    public string FunctionName { get; }

    public Dictionary<string, string> Parameters { get; } = new();

    private const string INPUT_PARAMETER_KEY = "input";

    public FunctionRunConfig()
        : this(string.Empty, string.Empty)
    {
    }

    public FunctionRunConfig(string pluginName, string functionName)
    {
        FunctionName = functionName;
        PluginName = pluginName;
        Parameters[INPUT_PARAMETER_KEY] = string.Empty;
    }

    public FunctionRunConfig(string pluginName, string functionName, Dictionary<string, string> parameters)
    : this(pluginName, functionName)
    {
        FunctionName = functionName;
        PluginName = pluginName;
        Parameters = parameters;
    }

    public void UpdateInput(string value)
    {
        Parameters[INPUT_PARAMETER_KEY] = value;
    }
}
