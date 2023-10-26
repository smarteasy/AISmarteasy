using AISmarteasy.Core.Connecting;
using AISmarteasy.Core.Prompt;

namespace AISmarteasy.Core.PluginFunction;

public abstract class Function
{
    public FunctionView View { get; }
    public string Name => View.Name;
    public string PluginName => View.PluginName;
    public string Description => View.Description;
    public IList<ParameterView> Parameters { get; set; }

    public AIRequestSettings RequestSettings { get; set; } = new();

    protected Function(string pluginName, string name, string description, bool isSemantic, IList<ParameterView>? parameters)
    {
        View = new FunctionView(pluginName, name, description, isSemantic, parameters);
        Parameters = View.Parameters;
    }

    protected Function()
    : this(string.Empty, string.Empty, string.Empty, true, null)
    {
    }

    public void SetAIConfiguration(AIRequestSettings? requestSettings)
    {
        Verify.NotNull(requestSettings);
        RequestSettings = requestSettings;
    }

    public abstract Task RunAsync(AIRequestSettings requestSettings, CancellationToken cancellationToken = default);
}
