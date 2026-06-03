using System;
using System.Net;
using Credfeto.Cache.Proxy.Server.Storage.Services;
using Credfeto.Cache.Proxy.Server.Storage.Services.LoggerExtensions;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Credfeto.Cache.Proxy.Server.Tests.Storage.Services.LoggerExtensions;

public sealed class ContentSourceLoggingExtensionsTests : LoggingTestBase
{
    public ContentSourceLoggingExtensionsTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public void UpstreamPackageFailed_DoesNotThrow()
    {
        ILogger<ContentSource> logger = this.GetTypedLogger<ContentSource>();

        logger.UpstreamPackageFailed(
            upstream: new Uri("http://upstream.example.com/package.nupkg"),
            statusCode: HttpStatusCode.NotFound
        );
    }

    [Fact]
    public void CachedPackage_DoesNotThrow()
    {
        ILogger<ContentSource> logger = this.GetTypedLogger<ContentSource>();

        logger.CachedPackage(upstream: new Uri("http://upstream.example.com/package.nupkg"), length: 1024);
    }
}
