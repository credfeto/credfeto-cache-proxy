using System;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Credfeto.Cache.Proxy.Server.Storage.Services.LoggerExtensions;

internal static partial class PackageDownloaderLoggingExtensions
{
    [LoggerMessage(
        LogLevel.Information,
        EventId = 2,
        Message = "Retrieved NUPKG from {upstream} Received Http {statusCode} Length: {length}"
    )]
    public static partial void UpstreamPackageOk(
        this ILogger<PackageDownloader> logger,
        Uri upstream,
        HttpStatusCode statusCode,
        int length
    );
}
