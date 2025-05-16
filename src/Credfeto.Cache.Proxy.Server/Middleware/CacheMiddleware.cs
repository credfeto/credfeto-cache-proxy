using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Models.Config;
using Credfeto.Cache.Proxy.Server.Extensions;
using Credfeto.Cache.Proxy.Server.Middleware.LoggingExtensions;
using Credfeto.Cache.Proxy.Server.Storage;
using Credfeto.Date.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Polly.Bulkhead;
using Polly.Timeout;

namespace Credfeto.Cache.Proxy.Server.Middleware;

public sealed class CacheMiddleware : IMiddleware
{
    private static readonly PathString PingPath = new("/ping");
    private readonly ServerConfig _config;
    private readonly IContentSource _contentSource;
    private readonly ICurrentTimeSource _currentTimeSource;
    private readonly ILogger<CacheMiddleware> _logger;

    public CacheMiddleware(ServerConfig config, IContentSource contentSource, ICurrentTimeSource currentTimeSource, ILogger<CacheMiddleware> logger)
    {
        this._config = config;
        this._contentSource = contentSource;
        this._currentTimeSource = currentTimeSource;
        this._logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (IsPingPath(context))
        {
            await next(context);

            return;
        }

        CacheServerConfig? config = this.GetConfig(context);

        if (config is null)
        {
            Failed(context: context, result: HttpStatusCode.NotFound);

            return;
        }

        string path = GetPath(context);
        string? query = context.Request.QueryString.Value;

        string pathWithQuery = GetPathWithQuery(query: query, path: path);

        this._logger.StartingFetch(host: config.Source, path: pathWithQuery);

        CancellationToken cancellationToken = context.RequestAborted;
        ProductInfoHeaderValue? userAgent = context.GetUserAgent();

        try
        {
            PackageResult? result = await this._contentSource.GetFromUpstreamAsync(config: config, path: pathWithQuery, userAgent: userAgent, cancellationToken: cancellationToken);

            if (result is null)
            {
                Failed(context: context, result: HttpStatusCode.NotFound);

                return;
            }

            await this.SuccessAsync(context: context, data: result.Value, cancellationToken: cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            Failed(context: context, exception.StatusCode ?? HttpStatusCode.InternalServerError);
        }
        catch (Exception exception) when (exception is TimeoutRejectedException or BulkheadRejectedException)
        {
            TooManyRequests(context);
        }
        catch (Exception exception)
        {
            this._logger.RequestFailed(host: config.Source, path: pathWithQuery, message: exception.Message, exception: exception);
            Failed(context: context, result: HttpStatusCode.NotFound);
        }
    }

    private static string GetPathWithQuery(string? query, string path)
    {
        return string.IsNullOrEmpty(query)
            ? path
            : path + "?" + query;
    }

    private static bool IsPingPath(HttpContext context)
    {
        return context.Request.Path == PingPath;
    }

    private CacheServerConfig? GetConfig(HttpContext context)
    {
        string host = GetHost(context);

        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        CacheServerConfig? config = this._config.Sites.FirstOrDefault(p => StringComparer.Ordinal.Equals(x: p.Source, y: host));

        if (config is null)
        {
            this._logger.NoConfigForHost(host: host);

            return null;
        }

        this._logger.UsingConfigForHost(host: host);

        return config;
    }

    private async ValueTask SuccessAsync(HttpContext context, PackageResult data, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.Headers.Append(key: "Content-Type", value: "application/octet-stream");
        context.Response.Headers.CacheControl = "public, max-age=63072000, immutable";
        context.Response.Headers.Expires = this._currentTimeSource.UtcNow()
                                               .AddSeconds(63072000)
                                               .ToString(format: "ddd, dd MMM yyyy HH:mm:ss 'GMT'", formatProvider: CultureInfo.InvariantCulture);

        await using (MemoryStream stream = new(buffer: data.Data, writable: false))
        {
            await stream.CopyToAsync(destination: context.Response.Body, cancellationToken: cancellationToken);
        }
    }

    private static void Failed(HttpContext context, HttpStatusCode result)
    {
        context.Response.StatusCode = (int)result;
        context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
    }

    private static void TooManyRequests(HttpContext context)
    {
        context.Response.Clear();
        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.Headers.Append(key: "Retry-After", value: "5");
    }

    private static string GetHost(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(key: "X-Forwarded-Host", out StringValues hostNames))
        {
            string? host = hostNames.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(host))
            {
                return host.Split(':')[0];
            }
        }

        return context.Request.Host.Host;
    }

    private static string GetPath(HttpContext context)
    {
        return context.Request.Path.HasValue
            ? context.Request.Path.Value
            : "/";
    }
}