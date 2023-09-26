namespace SemanticKernel.Function;

public interface IPluginCollection : IReadOnlyPluginCollection
{
    void AddFunction(ISKFunction function);
    IList<ISKFunction> GetAllFunctions();
}
