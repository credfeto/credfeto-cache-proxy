using Microsoft.Extensions.Logging;

namespace Credfeto.Cache.Proxy.Server.Middleware.LoggingExtensions;

internal static partial class CacheMiddlewareLoggingExtensions
{
    [LoggerMessage(LogLevel.Error, EventId = 1, Message = "Host is not configured: {host}")]
    public static partial void NoConfigForHost(this ILogger<CacheMiddleware> logger, string host);

    [LoggerMessage(LogLevel.Information, EventId = 2, Message = "Host is not configured: {host}")]
    public static partial void UsingConfigForHost(this ILogger<CacheMiddleware> logger, string host);

    [LoggerMessage(LogLevel.Information, EventId = 3, Message = "Starting fetch: {host}{path}")]
    public static partial void StartingFetch(this ILogger<CacheMiddleware> logger, string host, string path);
}