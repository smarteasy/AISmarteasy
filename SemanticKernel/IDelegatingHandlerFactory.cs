using Microsoft.Extensions.Logging;

namespace SemanticKernel;

public interface IDelegatingHandlerFactory
{
    DelegatingHandler Create(ILoggerFactory? loggerFactory);
}
