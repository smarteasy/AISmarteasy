namespace AISmarteasy.Core.PluginFunction;

public interface IPlugin
{
    void AddFunction(Function function);

    List<Function> Functions { get; }

    Function GetFunction(string functionName);
}