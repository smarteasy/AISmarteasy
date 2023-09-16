using SemanticKernel.Connector.OpenAI.TextCompletion;

namespace SemanticKernel.Service;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public class AIServiceCollection
{
    private const string DefaultKey = "__DEFAULT__";

   private readonly Dictionary<Type, Dictionary<string, Func<object>>> _services = new();

    private readonly Dictionary<Type, string> _defaultIds = new();

    public void SetService<T>(T service) where T : IAIService
        => SetService(DefaultKey, service, true);

    public void SetService<T>(string? name, T service, bool setAsDefault = false) where T : IAIService
        => SetService<T>(name, (() => service), setAsDefault);

    public void SetService<T>(Func<T> factory) where T : IAIService
        => SetService<T>(DefaultKey, factory, true);

    public void SetService<T>(string? name, Func<T> factory, bool setAsDefault = false) where T : IAIService
    {
        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        var type = typeof(T);
        if (!_services.TryGetValue(type, out var namedServices))
        {
            namedServices = new();
            _services[type] = namedServices;
        }

        if (name == null || setAsDefault || !this.HasDefault<T>())
        {
            _defaultIds[type] = name ?? DefaultKey;
        }

        var objectFactory = factory as Func<object>;

        namedServices[name ?? DefaultKey] = objectFactory
                                            ?? throw new InvalidOperationException("Service factory is an invalid format");
    }

    public IAIServiceProvider Build()
    {
        var servicesClone = this._services.ToDictionary(
            typeCollection => typeCollection.Key,
            typeCollection => typeCollection.Value.ToDictionary(
                service => service.Key,
                service => service.Value));

        var defaultsClone = this._defaultIds.ToDictionary(
            typeDefault => typeDefault.Key,
            typeDefault => typeDefault.Value);

        return new AIServiceProvider(servicesClone, defaultsClone);
    }

    private bool HasDefault<T>() where T : IAIService
        => this._defaultIds.TryGetValue(typeof(T), out var defaultName)
            && !string.IsNullOrEmpty(defaultName);
}
