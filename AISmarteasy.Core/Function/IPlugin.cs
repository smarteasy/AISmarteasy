using System.Diagnostics.CodeAnalysis;

namespace SemanticKernel.Function;

public interface IPlugin
{
    void AddFunction(ISKFunction function);

    List<ISKFunction> Functions { get; }

    ISKFunction GetFunction(string functionName);
}