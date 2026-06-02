using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Credfeto.Cache.Proxy.Server.Extensions;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Cache.Proxy.Server.Tests.Extensions;

public sealed class HttpClientExtensionsTests : TestBase
{
    [Fact]
    public void WithBaseAddress_SetsBaseAddress()
    {
        Uri uri = new("http://example.com/");

        using HttpClient client = new();
        HttpClient result = client.WithBaseAddress(uri);

        Assert.Equal(expected: uri, actual: result.BaseAddress);
    }

    [Fact]
    public void WithUserAgent_WhenNotNull_ClearsAndAddsUserAgent()
    {
        ProductInfoHeaderValue userAgent = new(new ProductHeaderValue(name: "TestClient", version: "1.0"));

        using HttpClient client = new();
        HttpClient result = client.WithUserAgent(userAgent);

        Assert.Contains(userAgent, result.DefaultRequestHeaders.UserAgent);
    }

    [Fact]
    public void WithUserAgent_WhenNull_LeavesUserAgentUnchanged()
    {
        using HttpClient client = new();
        int originalCount = client.DefaultRequestHeaders.UserAgent.Count;

        HttpClient result = client.WithUserAgent(null);

        Assert.Equal(expected: originalCount, actual: result.DefaultRequestHeaders.UserAgent.Count);
    }
}
