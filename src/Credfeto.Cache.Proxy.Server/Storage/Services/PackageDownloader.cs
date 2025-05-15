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

namespace Credfeto.Cache.Proxy.Server.Storage.Services;

public sealed class PackageDownloader : IPackageDownloader
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PackageDownloader> _logger;

    public PackageDownloader(IHttpClientFactory httpClientFactory, ILogger<PackageDownloader> logger)
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

        using (
            HttpResponseMessage result = await client.GetAsync(
                requestUri: requestUri,
                cancellationToken: cancellationToken
            )
        )
        {
            if (result.IsSuccessStatusCode)
            {
                byte[] bytes = await result.Content.ReadAsByteArrayAsync(cancellationToken: cancellationToken);

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

    private static Uri MakeUri(Uri baseUri, PathString path)
    {
        UriBuilder builder = new(baseUri) { Path = path.ToString() };

        return builder.Uri;
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
        baseUri = HttpClientNames.GetHttpClientUri(config: config, out string name);

        return this._httpClientFactory.CreateClient(name).WithBaseAddress(baseUri).WithUserAgent(userAgent);
    }
}
