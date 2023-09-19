using System.Diagnostics.CodeAnalysis;

namespace SemanticKernel.Function;

public interface IPluginCollection : IReadOnlyPluginCollection
{
    IPluginCollection AddFunction(ISKFunction function);
}
