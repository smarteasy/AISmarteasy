using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Handler;

public interface IDelegatingHandlerFactory
{
    DelegatingHandler Create(ILoggerFactory? loggerFactory);
}
