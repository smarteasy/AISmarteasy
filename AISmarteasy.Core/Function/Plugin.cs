using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISmarteasy.Core.Function;

public class Plugin : IPlugin
{
    public string Name { get; }
    private readonly ILogger _logger;
    private readonly Dictionary<string, ISKFunction> _functions;

    public Plugin(string name, ILoggerFactory? loggerFactory = null)
    {
        Name = name;
        _logger = loggerFactory is not null ? loggerFactory.CreateLogger(typeof(Plugin)) : NullLogger.Instance;
        _functions = new(StringComparer.OrdinalIgnoreCase);
    }

    public ISKFunction GetFunction(string functionName)
    {
        if (!_functions.TryGetValue(functionName, out var function))
        {
            ThrowFunctionNotAvailable(functionName);
        }

        return function!;
    }

    public void AddFunction(ISKFunction function)
    {
        Verify.NotNull(function);
        _functions[function.Name] = function;
    }

    public List<ISKFunction> Functions => _functions.Values.ToList();

    public PluginView BuildPluginView()
    {
        var result = new PluginView(Name);

        foreach (var function in Functions)
        {
            result.AddFunction(function.Describe());
        }

        return result;
    }

    private void ThrowFunctionNotAvailable(string functionName)
    {
        _logger.LogError("Function not available: {0}", functionName);
        throw new SKException($"Function not available {functionName}");
    }
}
