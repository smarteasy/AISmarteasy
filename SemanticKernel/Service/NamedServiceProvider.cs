namespace SemanticKernel.Service;

public class NamedServiceProvider<TService> : INamedServiceProvider<TService>
{
    private readonly Dictionary<Type, Dictionary<string, Func<object>>> _services;

    private readonly Dictionary<Type, string> _defaultIds;

    public NamedServiceProvider(
        Dictionary<Type, Dictionary<string, Func<object>>> services,
        Dictionary<Type, string> defaultIds)
    {
        this._services = services;
        this._defaultIds = defaultIds;
    }

    public T? GetService<T>(string? name = null) where T : TService
    {
        var factory = this.GetServiceFactory<T>(name);
        if (factory is Func<T>)
        {
            return factory.Invoke();
        }

        return default;
    }

    private string? GetDefaultServiceName<T>() where T : TService
    {
        var type = typeof(T);
        if (this._defaultIds.TryGetValue(type, out var name))
        {
            return name;
        }

        return null;
    }

    private Func<T>? GetServiceFactory<T>(string? name = null) where T : TService
    {
        if (this._services.TryGetValue(typeof(T), out var namedServices))
        {
            Func<object>? serviceFactory = null;

            name ??= this.GetDefaultServiceName<T>();
            if (name != null)
            {
                namedServices.TryGetValue(name, out serviceFactory);
            }

            return serviceFactory as Func<T>;
        }

        return null;
    }
}
