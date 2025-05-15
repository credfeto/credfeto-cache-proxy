using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Server.Config;
using Credfeto.Cache.Proxy.Server.Extensions;
using Credfeto.Cache.Proxy.Server.Storage;
using Credfeto.Date.Interfaces;
using Microsoft.AspNetCore.Http;
using Polly.Bulkhead;
using Polly.Timeout;

namespace Credfeto.Cache.Proxy.Server.Middleware;

public sealed class CacheMiddleware : IMiddleware
{
    private readonly ServerConfig _config;
    private readonly ICurrentTimeSource _currentTimeSource;
    private readonly INupkgSource _nupkgSource;

    public CacheMiddleware(ServerConfig config, INupkgSource nupkgSource, ICurrentTimeSource currentTimeSource)
    {
        this._config = config;
        this._nupkgSource = nupkgSource;
        this._currentTimeSource = currentTimeSource;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        string host = GetHost(context);

        if (string.IsNullOrWhiteSpace(host))
        {
            await next(context);

            return;
        }

        CacheServerConfig? config = this._config.Sites.FirstOrDefault(p =>
            StringComparer.Ordinal.Equals(x: p.Source, y: host)
        );

        if (config is null)
        {
            await next(context);

            return;
        }

        string path = GetPath(context);

        CancellationToken cancellationToken = context.RequestAborted;
        ProductInfoHeaderValue? userAgent = context.GetUserAgent();

        try
        {
            PackageResult? result = await this._nupkgSource.GetFromUpstreamAsync(
                config: config,
                path: path,
                userAgent: userAgent,
                cancellationToken: cancellationToken
            );

            if (result is null)
            {
                await next(context);

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
        catch (Exception)
        {
            await next(context);
        }
    }

    private async ValueTask SuccessAsync(HttpContext context, PackageResult data, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.Headers.Append(key: "Content-Type", value: "application/octet-stream");
        context.Response.Headers.CacheControl = "public, max-age=63072000, immutable";
        context.Response.Headers.Expires = this
            ._currentTimeSource.UtcNow()
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
        return context.Request.Host.Host;
    }

    private static string GetPath(HttpContext context)
    {
        return context.Request.Path.HasValue ? context.Request.Path.Value : "/";
    }
}
