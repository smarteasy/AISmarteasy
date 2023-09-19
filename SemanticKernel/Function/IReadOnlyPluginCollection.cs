using System.Diagnostics.CodeAnalysis;

namespace SemanticKernel.Function;

public interface IReadOnlyPluginCollection
{
    ISKFunction GetFunction(string functionName);

    ISKFunction GetFunction(string skillName, string functionName);

    bool TryGetFunction(string functionName, [NotNullWhen(true)] out ISKFunction? availableFunction);

    bool TryGetFunction(string skillName, string functionName, [NotNullWhen(true)] out ISKFunction? availableFunction);

    FunctionsView GetFunctionsView(bool includeSemantic = true, bool includeNative = true);
}
