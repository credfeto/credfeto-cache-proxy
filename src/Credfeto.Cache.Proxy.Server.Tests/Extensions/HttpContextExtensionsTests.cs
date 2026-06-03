using System.Net.Http.Headers;
using Credfeto.Cache.Proxy.Server.Extensions;
using FunFair.Test.Common;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Credfeto.Cache.Proxy.Server.Tests.Extensions;

public sealed class HttpContextExtensionsTests : TestBase
{
    [Fact]
    public void GetUserAgent_WhenMissingHeader_ReturnsNull()
    {
        DefaultHttpContext context = new();

        ProductInfoHeaderValue? result = context.GetUserAgent();

        Assert.Null(result);
    }

    [Fact]
    public void GetUserAgent_WhenWhitespaceHeader_ReturnsNull()
    {
        DefaultHttpContext context = new();
        context.Request.Headers.UserAgent = "   ";

        ProductInfoHeaderValue? result = context.GetUserAgent();

        Assert.Null(result);
    }

    [Fact]
    public void GetUserAgent_WhenUnparseable_ReturnsNull()
    {
        // A header with spaces/slashes but no valid product form fails TryParse
        DefaultHttpContext context = new();
        context.Request.Headers.UserAgent =
            "invalid header with / slash and spaces that is too complex for single product parse";

        ProductInfoHeaderValue? result = context.GetUserAgent();

        Assert.Null(result);
    }

    [Fact]
    public void GetUserAgent_WhenValidHeader_ReturnsParsedValue()
    {
        DefaultHttpContext context = new();
        context.Request.Headers.UserAgent = "TestClient/1.0";

        ProductInfoHeaderValue? result = context.GetUserAgent();

        Assert.NotNull(result);
        Assert.Equal(expected: "TestClient", actual: result.Product?.Name);
        Assert.Equal(expected: "1.0", actual: result.Product?.Version);
    }
}
