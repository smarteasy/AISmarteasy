using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Handling;

public interface IDelegatingHandlerFactory
{
    DelegatingHandler Create(ILoggerFactory? loggerFactory);
}
