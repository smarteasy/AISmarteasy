using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISmarteasy.Core.PluginFunction;

public class Plugin : IPlugin
{
    public string Name { get; init; } 

    private readonly ILogger _logger;
    private readonly Dictionary<string, Function> _functions;

    public Plugin(string name, ILoggerFactory? loggerFactory = null)
    {
        Name = name;

        _logger = loggerFactory is not null ? loggerFactory.CreateLogger(typeof(Plugin)) : NullLogger.Instance;
        _functions = new(StringComparer.OrdinalIgnoreCase);
    }

    public Function GetFunction(string functionName)
    {
        if (!_functions.TryGetValue(functionName, out var function))
        {
            ThrowFunctionNotAvailable(functionName);
        }

        return function!;
    }

    public void AddFunction(Function function)
    {
        Verify.NotNull(function);
        _functions[function.Name] = function;
    }

    public List<Function> Functions => _functions.Values.ToList();

    private void ThrowFunctionNotAvailable(string functionName)
    {
        _logger.LogError("Function not available: {0}", functionName);
        throw new SKException($"Function not available {functionName}");
    }
}
