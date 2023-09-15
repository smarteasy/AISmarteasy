using Microsoft.Extensions.Logging;

namespace SemanticKernel.Handler;

public interface IDelegatingHandlerFactory
{
    DelegatingHandler Create(ILoggerFactory? loggerFactory);
}
