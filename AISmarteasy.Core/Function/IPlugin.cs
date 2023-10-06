namespace AISmarteasy.Core.Function;

public interface IPlugin
{
    void AddFunction(ISKFunction function);

    List<ISKFunction> Functions { get; }

    ISKFunction GetFunction(string functionName);
}