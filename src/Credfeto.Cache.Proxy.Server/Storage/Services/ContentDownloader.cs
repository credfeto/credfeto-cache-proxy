using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Models.Config;
using Credfeto.Cache.Proxy.Server.Extensions;
using Credfeto.Cache.Proxy.Server.Storage.Services.LoggerExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NonBlocking;

namespace Credfeto.Cache.Proxy.Server.Storage.Services;

public sealed class ContentDownloader : IContentDownloader
{
    private static readonly CancellationToken DoNotCancelEarly = CancellationToken.None;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _connections = new(StringComparer.Ordinal);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ContentDownloader> _logger;

    public ContentDownloader(IHttpClientFactory httpClientFactory, ILogger<ContentDownloader> logger)
    {
        this._httpClientFactory = httpClientFactory;
        this._logger = logger;
    }

    public async ValueTask<byte[]> ReadUpstreamAsync(
        CacheServerConfig config,
        PathString path,
        ProductInfoHeaderValue? userAgent,
        CancellationToken cancellationToken
    )
    {
        HttpClient client = this.GetClient(config: config, userAgent: userAgent, out Uri baseUri);

        Uri requestUri = MakeUri(baseUri: baseUri, path: path);
        SemaphoreSlim? wait = await this.GetSemaphoreAsync(baseUri: requestUri, cancellationToken: cancellationToken);

        try
        {
            using (
                HttpResponseMessage result = await client.GetAsync(
                    requestUri: requestUri,
                    cancellationToken: DoNotCancelEarly
                )
            )
            {
                if (result.IsSuccessStatusCode)
                {
                    byte[] bytes = await result.Content.ReadAsByteArrayAsync(cancellationToken: DoNotCancelEarly);

                    this._logger.UpstreamPackageOk(
                        upstream: requestUri,
                        statusCode: result.StatusCode,
                        length: bytes.Length
                    );

                    return bytes;
                }

                return Failed(requestUri: requestUri, resultStatusCode: result.StatusCode);
            }
        }
        finally
        {
            wait?.Release();
        }
    }

    private async ValueTask<SemaphoreSlim?> GetSemaphoreAsync(Uri baseUri, CancellationToken cancellationToken)
    {
        if (!baseUri.PathAndQuery.EndsWith(value: ".gz", comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string key = baseUri.DnsSafeHost;

        if (this._connections.TryGetValue(key: key, out SemaphoreSlim? semaphore))
        {
            await semaphore.WaitAsync(cancellationToken);

            return semaphore;
        }

        semaphore = this._connections.GetOrAdd(key: key, new SemaphoreSlim(1));
        await semaphore.WaitAsync(cancellationToken);

        return semaphore;
    }

    private static Uri MakeUri(Uri baseUri, in PathString path)
    {
        string urlBase = baseUri.ToString();

        if (urlBase.EndsWith('/'))
        {
            urlBase = urlBase[..^1];
        }

        string full = urlBase + path.Value;


        return  new(full, UriKind.Absolute);
    }

    [DoesNotReturn]
    private static byte[] Failed(Uri requestUri, HttpStatusCode resultStatusCode)
    {
        throw new HttpRequestException(
            $"Failed to download {requestUri}: {resultStatusCode.GetName()}",
            inner: null,
            statusCode: resultStatusCode
        );
    }

    private HttpClient GetClient(CacheServerConfig config, ProductInfoHeaderValue? userAgent, out Uri baseUri)
    {
        baseUri = new(uriString: config.Target, uriKind: UriKind.Absolute);

        return this
            ._httpClientFactory.CreateClient(nameof(ContentDownloader))
            .WithBaseAddress(baseUri)
            .WithUserAgent(userAgent);
    }
}
