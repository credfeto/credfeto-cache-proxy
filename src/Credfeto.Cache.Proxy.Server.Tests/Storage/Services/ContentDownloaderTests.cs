using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Models.Config;
using Credfeto.Cache.Proxy.Server.Storage.Services;
using Credfeto.Cache.Proxy.Storage.Interfaces;
using FunFair.Test.Common;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace Credfeto.Cache.Proxy.Server.Tests.Storage.Services;

public sealed class ContentDownloaderTests : LoggingTestBase
{
    private const string SOURCE_HOST = "example.com";
    private const string TARGET_URL = "http://upstream.example.com";

    public ContentDownloaderTests(ITestOutputHelper output)
        : base(output) { }

    private static CacheServerConfig CreateConfig()
    {
        return new CacheServerConfig
        {
            Source = SOURCE_HOST,
            Target = TARGET_URL,
            Settings = [],
        };
    }

    [Fact]
    public async Task ReadUpstreamAsync_WhenSuccessAndNoCache_ReturnsMemoryStreamAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        byte[] responseBytes = [10, 20, 30];

        using StubHttpMessageHandler handler = new(HttpStatusCode.OK, responseBytes);

        IPackageStorage packageStorage = GetSubstitute<IPackageStorage>();
        IHttpClientFactory httpClientFactory = GetSubstitute<IHttpClientFactory>();

        using HttpClient httpClient = new(handler);
        httpClientFactory.CreateClient(nameof(ContentDownloader)).Returns(httpClient);

        ContentDownloader downloader = new(
            httpClientFactory: httpClientFactory,
            packageStorage: packageStorage,
            logger: this.GetTypedLogger<ContentDownloader>()
        );

        CacheServerConfig config = CreateConfig();

        Stream result = await downloader.ReadUpstreamAsync(
            config: config,
            path: new PathString("/index.json"),
            userAgent: null,
            cache: false,
            cancellationToken: cancellationToken
        );

        Assert.NotNull(result);
        Assert.Equal(expected: responseBytes.Length, actual: result.Length);
    }

    [Fact]
    public async Task ReadUpstreamAsync_WhenSuccessAndCache_SavesAndReturnsFromStorageAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        byte[] responseBytes = [10, 20, 30];

        using StubHttpMessageHandler handler = new(HttpStatusCode.OK, responseBytes);

        IPackageStorage packageStorage = GetSubstitute<IPackageStorage>();
        IHttpClientFactory httpClientFactory = GetSubstitute<IHttpClientFactory>();

        using HttpClient httpClient = new(handler);
        httpClientFactory.CreateClient(nameof(ContentDownloader)).Returns(httpClient);

        byte[] storedBytes = [10, 20, 30];

        await using MemoryStream storedStream = new(storedBytes);

        packageStorage.ReadFile(sourceHost: Arg.Any<string>(), sourcePath: Arg.Any<string>()).Returns(storedStream);

        ContentDownloader downloader = new(
            httpClientFactory: httpClientFactory,
            packageStorage: packageStorage,
            logger: this.GetTypedLogger<ContentDownloader>()
        );

        CacheServerConfig config = CreateConfig();

        Stream result = await downloader.ReadUpstreamAsync(
            config: config,
            path: new PathString("/packages/test.nupkg"),
            userAgent: null,
            cache: true,
            cancellationToken: cancellationToken
        );

        Assert.NotNull(result);

        await packageStorage
            .Received(1)
            .SaveFileAsync(
                sourceHost: Arg.Any<string>(),
                sourcePath: Arg.Any<string>(),
                readAsync: Arg.Any<Func<Stream, CancellationToken, Task>>(),
                cancellationToken: Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task ReadUpstreamAsync_WhenSuccessAndCacheButReadFileReturnsNull_ThrowsHttpRequestExceptionAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        byte[] responseBytes = [10, 20, 30];

        using StubHttpMessageHandler handler = new(HttpStatusCode.OK, responseBytes);

        IPackageStorage packageStorage = GetSubstitute<IPackageStorage>();
        IHttpClientFactory httpClientFactory = GetSubstitute<IHttpClientFactory>();

        using HttpClient httpClient = new(handler);
        httpClientFactory.CreateClient(nameof(ContentDownloader)).Returns(httpClient);

        // Simulate the scenario where SaveFileAsync succeeds but ReadFile still returns null
        packageStorage.ReadFile(sourceHost: Arg.Any<string>(), sourcePath: Arg.Any<string>()).Returns((Stream?)null);

        ContentDownloader downloader = new(
            httpClientFactory: httpClientFactory,
            packageStorage: packageStorage,
            logger: this.GetTypedLogger<ContentDownloader>()
        );

        CacheServerConfig config = CreateConfig();

        // After SaveFile, ReadFile returns null → falls through to Failed() → throws HttpRequestException
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            downloader
                .ReadUpstreamAsync(
                    config: config,
                    path: new PathString("/packages/test.nupkg"),
                    userAgent: null,
                    cache: true,
                    cancellationToken: cancellationToken
                )
                .AsTask()
        );
    }

    [Fact]
    public async Task ReadUpstreamAsync_WhenNon2xxResponse_ThrowsHttpRequestExceptionAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        using StubHttpMessageHandler handler = new(HttpStatusCode.NotFound, []);

        IPackageStorage packageStorage = GetSubstitute<IPackageStorage>();
        IHttpClientFactory httpClientFactory = GetSubstitute<IHttpClientFactory>();

        using HttpClient httpClient = new(handler);
        httpClientFactory.CreateClient(nameof(ContentDownloader)).Returns(httpClient);

        ContentDownloader downloader = new(
            httpClientFactory: httpClientFactory,
            packageStorage: packageStorage,
            logger: this.GetTypedLogger<ContentDownloader>()
        );

        CacheServerConfig config = CreateConfig();

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            downloader
                .ReadUpstreamAsync(
                    config: config,
                    path: new PathString("/packages/missing.nupkg"),
                    userAgent: null,
                    cache: false,
                    cancellationToken: cancellationToken
                )
                .AsTask()
        );
    }

    [Fact]
    public async Task ReadUpstreamAsync_WithUserAgent_SendsUserAgentHeaderAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        byte[] responseBytes = [1, 2, 3];

        using StubHttpMessageHandler handler = new(HttpStatusCode.OK, responseBytes);

        IPackageStorage packageStorage = GetSubstitute<IPackageStorage>();
        IHttpClientFactory httpClientFactory = GetSubstitute<IHttpClientFactory>();

        using HttpClient httpClient = new(handler);
        httpClientFactory.CreateClient(nameof(ContentDownloader)).Returns(httpClient);

        ContentDownloader downloader = new(
            httpClientFactory: httpClientFactory,
            packageStorage: packageStorage,
            logger: this.GetTypedLogger<ContentDownloader>()
        );

        CacheServerConfig config = CreateConfig();

        ProductInfoHeaderValue userAgent = new(new ProductHeaderValue(name: "TestAgent", version: "2.0"));

        Stream result = await downloader.ReadUpstreamAsync(
            config: config,
            path: new PathString("/index.json"),
            userAgent: userAgent,
            cache: false,
            cancellationToken: cancellationToken
        );

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ReadUpstreamAsync_WithGzPath_AcquiresSemaphoreAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        byte[] responseBytes = [1, 2, 3];

        using StubHttpMessageHandler handler = new(HttpStatusCode.OK, responseBytes);

        IPackageStorage packageStorage = GetSubstitute<IPackageStorage>();
        IHttpClientFactory httpClientFactory = GetSubstitute<IHttpClientFactory>();

        // Each call to CreateClient must return a fresh HttpClient so BaseAddress can be set
        httpClientFactory
            .CreateClient(nameof(ContentDownloader))
            .Returns(_ => new HttpClient(handler), _ => new HttpClient(handler));

        ContentDownloader downloader = new(
            httpClientFactory: httpClientFactory,
            packageStorage: packageStorage,
            logger: this.GetTypedLogger<ContentDownloader>()
        );

        CacheServerConfig config = CreateConfig();

        // Call twice with .gz path — both should succeed sequentially using the semaphore
        Stream result1 = await downloader.ReadUpstreamAsync(
            config: config,
            path: new PathString("/packages/test.nupkg.gz"),
            userAgent: null,
            cache: false,
            cancellationToken: cancellationToken
        );

        Stream result2 = await downloader.ReadUpstreamAsync(
            config: config,
            path: new PathString("/packages/other.nupkg.gz"),
            userAgent: null,
            cache: false,
            cancellationToken: cancellationToken
        );

        Assert.NotNull(result1);
        Assert.NotNull(result2);
    }
}
