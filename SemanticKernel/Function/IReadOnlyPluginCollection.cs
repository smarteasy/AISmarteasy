using System.Diagnostics.CodeAnalysis;

namespace SemanticKernel.Function;

public interface IReadOnlyPluginCollection
{
    ISKFunction GetFunction(string pluginName, string functionName);

    FunctionsView GetFunctionsView();

    bool TryGetFunction(string skillName, string functionName, [NotNullWhen(true)] out ISKFunction? availableFunction);
}
