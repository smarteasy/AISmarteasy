using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Util;

public static class ConsoleLogger
{
    internal static ILogger Logger => LoggerFactory.CreateLogger<object>();

    internal static ILoggerFactory LoggerFactory => Factory.Value;

    private static readonly Lazy<ILoggerFactory> Factory = new(LogBuilder);

    private static ILoggerFactory LogBuilder()
    {
        return Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Warning);

            builder.AddFilter("Microsoft", LogLevel.Warning);
            builder.AddFilter("System", LogLevel.Warning);

            builder.AddConsole();
        });
    }
}