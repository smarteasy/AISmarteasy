namespace SemanticKernel.Service;

public class AIServiceProvider : NamedServiceProvider<IAIService>, IAIServiceProvider
{
    public AIServiceProvider(Dictionary<Type, Dictionary<string, Func<object>>> services, Dictionary<Type, string> defaultIds)
        : base(services, defaultIds)
    {
    }
}
