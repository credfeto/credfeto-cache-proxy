using System;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Credfeto.Cache.Proxy.Server.Storage.Services.LoggerExtensions;

internal static partial class NupkgSourceLoggingExtensions
{
    [LoggerMessage(
        LogLevel.Error,
        EventId = 1,
        Message = "Failed to retrieve NUPKG from {upstream} Received Http {statusCode}"
    )]
    public static partial void UpstreamPackageFailed(
        this ILogger<ContentSource> logger,
        Uri upstream,
        HttpStatusCode statusCode
    );

    [LoggerMessage(
        LogLevel.Information,
        EventId = 1,
        Message = "Retrieved Cached NUPKG from {upstream} Length: {length}"
    )]
    public static partial void CachedPackage(this ILogger<ContentSource> logger, Uri upstream, long length);
}
