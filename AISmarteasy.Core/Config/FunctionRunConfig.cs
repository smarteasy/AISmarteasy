﻿namespace AISmarteasy.Core.Config;

public readonly struct FunctionRunConfig
{
    public string FunctionName { get; }

    public string PluginName { get; }

    public Dictionary<string, string> Parameters { get;} = new Dictionary<string, string>();

    private const string INPUT_PARAMETER_KEY = "input";

    public FunctionRunConfig(string pluginName, string functionName)
    {
        FunctionName = functionName;
        PluginName = pluginName;

        Parameters[INPUT_PARAMETER_KEY] = string.Empty;
    }

    public void UpdateInput(string value)
    {
        Parameters[INPUT_PARAMETER_KEY] = value;
    }


}
