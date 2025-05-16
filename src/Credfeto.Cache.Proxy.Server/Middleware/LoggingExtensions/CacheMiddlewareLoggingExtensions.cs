using System;
using Microsoft.Extensions.Logging;

namespace Credfeto.Cache.Proxy.Server.Middleware.LoggingExtensions;

internal static partial class CacheMiddlewareLoggingExtensions
{
    [LoggerMessage(LogLevel.Error, EventId = 1, Message = "Host is not configured: https://{host}")]
    public static partial void NoConfigForHost(this ILogger<CacheMiddleware> logger, string host);

    [LoggerMessage(LogLevel.Information, EventId = 2, Message = "Using Configured Host Config: http://{host}")]
    public static partial void UsingConfigForHost(this ILogger<CacheMiddleware> logger, string host);

    [LoggerMessage(LogLevel.Information, EventId = 3, Message = "Starting fetch: https://{host}{path}")]
    public static partial void StartingFetch(this ILogger<CacheMiddleware> logger, string host, string path);

    [LoggerMessage(LogLevel.Information, EventId = 4, Message = "Request failed: https://{host}{path} -> {message}")]
    public static partial void RequestFailed(
        this ILogger<CacheMiddleware> logger,
        string host,
        string path,
        string message,
        Exception exception
    );
}
