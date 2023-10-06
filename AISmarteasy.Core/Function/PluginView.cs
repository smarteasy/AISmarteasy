namespace AISmarteasy.Core.Function;

public sealed class PluginView
{
    public string Name { get; }

    public PluginView(string name)
    {
        Name = name;
    }

    public Dictionary<string, FunctionView> FunctionViews { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);

    public void AddFunction(FunctionView functionView)
    {
        FunctionViews.Add(functionView.Name, functionView);
    }
}
