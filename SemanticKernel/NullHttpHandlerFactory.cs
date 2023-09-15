using Microsoft.Extensions.Logging;

namespace SemanticKernel;

public sealed class NullHttpHandlerFactory : IDelegatingHandlerFactory
{
    public static NullHttpHandlerFactory Instance => new();

    public DelegatingHandler Create(ILoggerFactory? loggerFactory)
    {
        return new NullHttpHandler();
    }
}
