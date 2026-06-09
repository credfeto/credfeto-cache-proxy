using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Models.Config;
using Credfeto.Cache.Proxy.Server.Storage;
using Credfeto.Cache.Proxy.Server.Storage.Services;
using Credfeto.Cache.Proxy.Storage.Interfaces;
using FunFair.Test.Common;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace Credfeto.Cache.Proxy.Server.Tests.Storage.Services;

public sealed class ContentSourceTests : LoggingTestBase
{
    private const string SOURCE_HOST = "example.com";
    private const string TARGET_URL = "http://example.com";

    public ContentSourceTests(ITestOutputHelper output)
        : base(output) { }

    private static CacheServerConfig CreateConfig(List<CacheSetting>? settings = null)
    {
        return new CacheServerConfig
        {
            Source = SOURCE_HOST,
            Target = TARGET_URL,
            Settings = settings ?? [],
        };
    }

    [Fact]
    public async Task GetFromUpstreamAsync_WhenCachedFileExists_ReturnsCachedResultAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        IPackageStorage packageStorage = GetSubstitute<IPackageStorage>();
        IContentDownloader contentDownloader = GetSubstitute<IContentDownloader>();

        byte[] bytes = [1, 2, 3];

        await using MemoryStream cachedStream = new(bytes);

        packageStorage.ReadFile(sourceHost: Arg.Any<string>(), sourcePath: Arg.Any<string>()).Returns(cachedStream);

        ContentSource contentSource = new(packageStorage: packageStorage, contentDownloader: contentDownloader);

        CacheServerConfig config = CreateConfig();

        PackageResult? result = await contentSource.GetFromUpstreamAsync(
            config: config,
            path: "/packages/test.nupkg",
            userAgent: null,
            cancellationToken: cancellationToken
        );

        Assert.NotNull(result);
        Assert.Same(expected: cachedStream, actual: result.Value.Data);
    }

    [Fact]
    public async Task GetFromUpstreamAsync_WhenPathHasQuery_BypassesCacheAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        IPackageStorage packageStorage = GetSubstitute<IPackageStorage>();
        IContentDownloader contentDownloader = GetSubstitute<IContentDownloader>();

        byte[] bytes = [1, 2, 3];

        await using MemoryStream upstreamStream = new(bytes);

        contentDownloader
            .ReadUpstreamAsync(
                config: Arg.Any<CacheServerConfig>(),
                path: Arg.Any<PathString>(),
                userAgent: Arg.Any<ProductInfoHeaderValue?>(),
                cache: Arg.Any<bool>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns(upstreamStream);

        ContentSource contentSource = new(packageStorage: packageStorage, contentDownloader: contentDownloader);

        CacheServerConfig config = CreateConfig();

        PackageResult? result = await contentSource.GetFromUpstreamAsync(
            config: config,
            path: "/packages/test.nupkg?version=1.0",
            userAgent: null,
            cancellationToken: cancellationToken
        );

        Assert.NotNull(result);
        Assert.Same(expected: upstreamStream, actual: result.Value.Data);

        // Storage should NOT have been consulted for a cached file when there's a query string
        _ = packageStorage.DidNotReceive().ReadFile(sourceHost: Arg.Any<string>(), sourcePath: Arg.Any<string>());
    }

    [Fact]
    public async Task GetFromUpstreamAsync_WhenNoMatchingCacheSetting_CacheFlagIsFalseAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        IPackageStorage packageStorage = GetSubstitute<IPackageStorage>();
        IContentDownloader contentDownloader = GetSubstitute<IContentDownloader>();

        packageStorage.ReadFile(sourceHost: Arg.Any<string>(), sourcePath: Arg.Any<string>()).Returns((Stream?)null);

        byte[] bytes = [1, 2, 3];

        await using MemoryStream upstreamStream = new(bytes);

        contentDownloader
            .ReadUpstreamAsync(
                config: Arg.Any<CacheServerConfig>(),
                path: Arg.Any<PathString>(),
                userAgent: Arg.Any<ProductInfoHeaderValue?>(),
                cache: false,
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns(upstreamStream);

        ContentSource contentSource = new(packageStorage: packageStorage, contentDownloader: contentDownloader);

        // Config with no cache settings → nothing matches → cache=false
        CacheServerConfig config = CreateConfig(settings: []);

        PackageResult? result = await contentSource.GetFromUpstreamAsync(
            config: config,
            path: "/packages/test.nupkg",
            userAgent: null,
            cancellationToken: cancellationToken
        );

        Assert.NotNull(result);
        Assert.Null(result.Value.CacheSetting);

        _ = await contentDownloader
            .Received(1)
            .ReadUpstreamAsync(
                config: Arg.Any<CacheServerConfig>(),
                path: Arg.Any<PathString>(),
                userAgent: Arg.Any<ProductInfoHeaderValue?>(),
                cache: false,
                cancellationToken: Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task GetFromUpstreamAsync_WhenMatchingCacheSetting_CacheFlagIsTrueAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        IPackageStorage packageStorage = GetSubstitute<IPackageStorage>();
        IContentDownloader contentDownloader = GetSubstitute<IContentDownloader>();

        packageStorage.ReadFile(sourceHost: Arg.Any<string>(), sourcePath: Arg.Any<string>()).Returns((Stream?)null);

        byte[] bytes = [1, 2, 3];

        await using MemoryStream upstreamStream = new(bytes);

        contentDownloader
            .ReadUpstreamAsync(
                config: Arg.Any<CacheServerConfig>(),
                path: Arg.Any<PathString>(),
                userAgent: Arg.Any<ProductInfoHeaderValue?>(),
                cache: true,
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns(upstreamStream);

        CacheSetting matchAllSetting = new() { Match = ".*\\.nupkg$", LifeTimeSeconds = 3600 };
        ContentSource contentSource = new(packageStorage: packageStorage, contentDownloader: contentDownloader);

        CacheServerConfig config = CreateConfig(settings: [matchAllSetting]);

        PackageResult? result = await contentSource.GetFromUpstreamAsync(
            config: config,
            path: "/packages/test.nupkg",
            userAgent: null,
            cancellationToken: cancellationToken
        );

        Assert.NotNull(result);
        Assert.NotNull(result.Value.CacheSetting);

        _ = await contentDownloader
            .Received(1)
            .ReadUpstreamAsync(
                config: Arg.Any<CacheServerConfig>(),
                path: Arg.Any<PathString>(),
                userAgent: Arg.Any<ProductInfoHeaderValue?>(),
                cache: true,
                cancellationToken: Arg.Any<CancellationToken>()
            );
    }
}
