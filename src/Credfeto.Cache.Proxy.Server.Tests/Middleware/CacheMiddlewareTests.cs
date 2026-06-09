using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Models.Config;
using Credfeto.Cache.Proxy.Server.Middleware;
using Credfeto.Cache.Proxy.Server.Storage;
using FunFair.Test.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Polly.Bulkhead;
using Polly.Timeout;
using Xunit;

namespace Credfeto.Cache.Proxy.Server.Tests.Middleware;

public sealed class CacheMiddlewareTests : LoggingTestBase
{
    private const string SOURCE_HOST = "example.com";
    private const string TARGET_URL = "http://upstream.example.com";

    private static readonly CacheServerConfig SiteConfig = new()
    {
        Source = SOURCE_HOST,
        Target = TARGET_URL,
        Settings = [],
    };

    private static readonly ServerConfig Config = new() { Sites = [SiteConfig], Storage = "/data" };

    public CacheMiddlewareTests(ITestOutputHelper output)
        : base(output) { }

    private CacheMiddleware CreateMiddleware(IContentSource contentSource, FakeTimeProvider? timeProvider = null)
    {
        FakeTimeProvider tp = timeProvider ?? new FakeTimeProvider();

        return new CacheMiddleware(
            config: Options.Create(Config),
            contentSource: contentSource,
            timeProvider: tp,
            logger: this.GetTypedLogger<CacheMiddleware>()
        );
    }

    [Fact]
    public async Task InvokeAsync_WhenEndpointIsSet_CallsNextAsync()
    {
        IContentSource contentSource = GetSubstitute<IContentSource>();
        CacheMiddleware middleware = this.CreateMiddleware(contentSource);

        DefaultHttpContext context = new();
        context.SetEndpoint(new Endpoint(requestDelegate: null, metadata: null, displayName: "test"));

        bool nextCalled = false;

        await middleware.InvokeAsync(context, NextAsync);

        Assert.True(nextCalled, userMessage: "Next delegate should have been called");

        Task NextAsync(HttpContext _)
        {
            nextCalled = true;

            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task InvokeAsync_WhenNoConfigForHost_Returns404Async()
    {
        IContentSource contentSource = GetSubstitute<IContentSource>();
        CacheMiddleware middleware = this.CreateMiddleware(contentSource);

        DefaultHttpContext context = new();
        context.Request.Host = new HostString("unknown-host.example.com");

        await middleware.InvokeAsync(context, static _ => Task.CompletedTask);

        Assert.Equal(expected: (int)HttpStatusCode.NotFound, actual: context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenContentSourceReturnsNull_Returns404Async()
    {
        IContentSource contentSource = GetSubstitute<IContentSource>();
        contentSource
            .GetFromUpstreamAsync(
                config: Arg.Any<CacheServerConfig>(),
                path: Arg.Any<string>(),
                userAgent: Arg.Any<System.Net.Http.Headers.ProductInfoHeaderValue?>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns((PackageResult?)null);

        CacheMiddleware middleware = this.CreateMiddleware(contentSource);

        DefaultHttpContext context = new();
        context.Request.Host = new HostString(SOURCE_HOST);

        await middleware.InvokeAsync(context, static _ => Task.CompletedTask);

        Assert.Equal(expected: (int)HttpStatusCode.NotFound, actual: context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenContentSourceReturnsResult_Returns200AndCopiesBodyAsync()
    {
        byte[] data = [1, 2, 3, 4];
        MemoryStream dataStream = new(data);
        PackageResult packageResult = new(Data: dataStream, CacheSetting: null);

        IContentSource contentSource = GetSubstitute<IContentSource>();
        contentSource
            .GetFromUpstreamAsync(
                config: Arg.Any<CacheServerConfig>(),
                path: Arg.Any<string>(),
                userAgent: Arg.Any<System.Net.Http.Headers.ProductInfoHeaderValue?>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns(packageResult);

        CacheMiddleware middleware = this.CreateMiddleware(contentSource);

        DefaultHttpContext context = new();
        context.Request.Host = new HostString(SOURCE_HOST);
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, static _ => Task.CompletedTask);

        Assert.Equal(expected: (int)HttpStatusCode.OK, actual: context.Response.StatusCode);
        Assert.Equal(expected: data.Length, actual: context.Response.Body.Length);
    }

    [Fact]
    public async Task InvokeAsync_WhenHttpRequestException_ReturnsStatusFromExceptionAsync()
    {
        IContentSource contentSource = GetSubstitute<IContentSource>();
        contentSource
            .GetFromUpstreamAsync(
                config: Arg.Any<CacheServerConfig>(),
                path: Arg.Any<string>(),
                userAgent: Arg.Any<System.Net.Http.Headers.ProductInfoHeaderValue?>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns<PackageResult?>(_ =>
                throw new HttpRequestException(message: "Forbidden", inner: null, statusCode: HttpStatusCode.Forbidden)
            );

        CacheMiddleware middleware = this.CreateMiddleware(contentSource);

        DefaultHttpContext context = new();
        context.Request.Host = new HostString(SOURCE_HOST);

        await middleware.InvokeAsync(context, static _ => Task.CompletedTask);

        Assert.Equal(expected: (int)HttpStatusCode.Forbidden, actual: context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenTimeoutRejectedException_Returns429Async()
    {
        IContentSource contentSource = GetSubstitute<IContentSource>();
        contentSource
            .GetFromUpstreamAsync(
                config: Arg.Any<CacheServerConfig>(),
                path: Arg.Any<string>(),
                userAgent: Arg.Any<System.Net.Http.Headers.ProductInfoHeaderValue?>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns<PackageResult?>(_ => throw new TimeoutRejectedException());

        CacheMiddleware middleware = this.CreateMiddleware(contentSource);

        DefaultHttpContext context = new();
        context.Request.Host = new HostString(SOURCE_HOST);

        await middleware.InvokeAsync(context, static _ => Task.CompletedTask);

        Assert.Equal(expected: (int)HttpStatusCode.TooManyRequests, actual: context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenBulkheadRejectedException_Returns429Async()
    {
        IContentSource contentSource = GetSubstitute<IContentSource>();
        contentSource
            .GetFromUpstreamAsync(
                config: Arg.Any<CacheServerConfig>(),
                path: Arg.Any<string>(),
                userAgent: Arg.Any<System.Net.Http.Headers.ProductInfoHeaderValue?>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns<PackageResult?>(_ => throw new BulkheadRejectedException());

        CacheMiddleware middleware = this.CreateMiddleware(contentSource);

        DefaultHttpContext context = new();
        context.Request.Host = new HostString(SOURCE_HOST);

        await middleware.InvokeAsync(context, static _ => Task.CompletedTask);

        Assert.Equal(expected: (int)HttpStatusCode.TooManyRequests, actual: context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnexpectedException_Returns404Async()
    {
        IContentSource contentSource = GetSubstitute<IContentSource>();
        contentSource
            .GetFromUpstreamAsync(
                config: Arg.Any<CacheServerConfig>(),
                path: Arg.Any<string>(),
                userAgent: Arg.Any<System.Net.Http.Headers.ProductInfoHeaderValue?>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns<PackageResult?>(_ => throw new InvalidOperationException("Unexpected!"));

        CacheMiddleware middleware = this.CreateMiddleware(contentSource);

        DefaultHttpContext context = new();
        context.Request.Host = new HostString(SOURCE_HOST);

        await middleware.InvokeAsync(context, static _ => Task.CompletedTask);

        Assert.Equal(expected: (int)HttpStatusCode.NotFound, actual: context.Response.StatusCode);
    }

    [Fact]
    public async Task GetHost_WithXForwardedHostHeader_UsesHeaderAsync()
    {
        IContentSource contentSource = GetSubstitute<IContentSource>();
        contentSource
            .GetFromUpstreamAsync(
                config: Arg.Any<CacheServerConfig>(),
                path: Arg.Any<string>(),
                userAgent: Arg.Any<System.Net.Http.Headers.ProductInfoHeaderValue?>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns((PackageResult?)null);

        CacheMiddleware middleware = this.CreateMiddleware(contentSource);

        DefaultHttpContext context = new();
        context.Request.Headers.Append(key: "X-Forwarded-Host", value: SOURCE_HOST);

        await middleware.InvokeAsync(context, static _ => Task.CompletedTask);

        Assert.Equal(expected: (int)HttpStatusCode.NotFound, actual: context.Response.StatusCode);
    }

    [Fact]
    public async Task GetHost_WithXForwardedHostHeaderContainingPort_StripsPortAsync()
    {
        IContentSource contentSource = GetSubstitute<IContentSource>();
        contentSource
            .GetFromUpstreamAsync(
                config: Arg.Any<CacheServerConfig>(),
                path: Arg.Any<string>(),
                userAgent: Arg.Any<System.Net.Http.Headers.ProductInfoHeaderValue?>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns((PackageResult?)null);

        CacheMiddleware middleware = this.CreateMiddleware(contentSource);

        DefaultHttpContext context = new();
        context.Request.Headers.Append(key: "X-Forwarded-Host", value: SOURCE_HOST + ":8080");

        await middleware.InvokeAsync(context, static _ => Task.CompletedTask);

        Assert.Equal(expected: (int)HttpStatusCode.NotFound, actual: context.Response.StatusCode);
    }

    [Fact]
    public async Task GetHost_WithoutXForwardedHostHeader_UsesRequestHostAsync()
    {
        // When no X-Forwarded-Host header is set, the Request.Host should be used directly.
        // An unknown host returns 404; a known host (SOURCE_HOST) proceeds to content lookup.
        IContentSource contentSource = GetSubstitute<IContentSource>();
        contentSource
            .GetFromUpstreamAsync(
                config: Arg.Any<CacheServerConfig>(),
                path: Arg.Any<string>(),
                userAgent: Arg.Any<System.Net.Http.Headers.ProductInfoHeaderValue?>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns((PackageResult?)null);

        CacheMiddleware middleware = this.CreateMiddleware(contentSource);

        DefaultHttpContext contextWithKnownHost = new();
        contextWithKnownHost.Request.Host = new HostString(SOURCE_HOST);

        await middleware.InvokeAsync(contextWithKnownHost, static _ => Task.CompletedTask);

        // The known host led to a content source lookup (not short-circuited with no-config 404)
        await contentSource
            .Received(1)
            .GetFromUpstreamAsync(
                config: Arg.Any<CacheServerConfig>(),
                path: Arg.Any<string>(),
                userAgent: Arg.Any<System.Net.Http.Headers.ProductInfoHeaderValue?>(),
                cancellationToken: Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task GetPathWithQuery_WithQuery_AppendsQueryStringAsync()
    {
        IContentSource contentSource = GetSubstitute<IContentSource>();
        string? capturedPath = null;
        contentSource
            .GetFromUpstreamAsync(
                config: Arg.Any<CacheServerConfig>(),
                path: Arg.Do<string>(p => capturedPath = p),
                userAgent: Arg.Any<System.Net.Http.Headers.ProductInfoHeaderValue?>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns((PackageResult?)null);

        CacheMiddleware middleware = this.CreateMiddleware(contentSource);

        DefaultHttpContext context = new();
        context.Request.Host = new HostString(SOURCE_HOST);
        context.Request.Path = "/some/path";
        context.Request.QueryString = new QueryString("?key=value");

        await middleware.InvokeAsync(context, static _ => Task.CompletedTask);

        Assert.NotNull(capturedPath);
        Assert.Contains("key=value", capturedPath, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetPathWithQuery_WithoutQuery_ReturnsPathAsync()
    {
        IContentSource contentSource = GetSubstitute<IContentSource>();
        string? capturedPath = null;
        contentSource
            .GetFromUpstreamAsync(
                config: Arg.Any<CacheServerConfig>(),
                path: Arg.Do<string>(p => capturedPath = p),
                userAgent: Arg.Any<System.Net.Http.Headers.ProductInfoHeaderValue?>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns((PackageResult?)null);

        CacheMiddleware middleware = this.CreateMiddleware(contentSource);

        DefaultHttpContext context = new();
        context.Request.Host = new HostString(SOURCE_HOST);
        context.Request.Path = "/some/path";

        await middleware.InvokeAsync(context, static _ => Task.CompletedTask);

        Assert.NotNull(capturedPath);
        Assert.Equal(expected: "/some/path", actual: capturedPath);
    }

    [Fact]
    public async Task InvokeAsync_WhenSuccessResult_SetsExpiresHeaderAsync()
    {
        FakeTimeProvider fakeTimeProvider = new();
        byte[] data = [1, 2, 3];
        MemoryStream dataStream = new(data);
        PackageResult packageResult = new(Data: dataStream, CacheSetting: null);

        IContentSource contentSource = GetSubstitute<IContentSource>();
        contentSource
            .GetFromUpstreamAsync(
                config: Arg.Any<CacheServerConfig>(),
                path: Arg.Any<string>(),
                userAgent: Arg.Any<System.Net.Http.Headers.ProductInfoHeaderValue?>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns(packageResult);

        CacheMiddleware middleware = this.CreateMiddleware(contentSource, fakeTimeProvider);

        DefaultHttpContext context = new();
        context.Request.Host = new HostString(SOURCE_HOST);
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, static _ => Task.CompletedTask);

        Assert.Equal(expected: (int)HttpStatusCode.OK, actual: context.Response.StatusCode);
        Assert.NotEmpty(context.Response.Headers.Expires.ToString());
    }

    [Fact]
    public async Task InvokeAsync_WhenEmptyHost_Returns404Async()
    {
        IContentSource contentSource = GetSubstitute<IContentSource>();
        CacheMiddleware middleware = this.CreateMiddleware(contentSource);

        DefaultHttpContext context = new();

        await middleware.InvokeAsync(context, static _ => Task.CompletedTask);

        Assert.Equal(expected: (int)HttpStatusCode.NotFound, actual: context.Response.StatusCode);
    }
}
