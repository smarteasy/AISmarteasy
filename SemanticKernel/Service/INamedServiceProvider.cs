namespace SemanticKernel.Service;

public interface INamedServiceProvider<in TService>
{
    T? GetService<T>(string? name = null) where T : TService;
}
