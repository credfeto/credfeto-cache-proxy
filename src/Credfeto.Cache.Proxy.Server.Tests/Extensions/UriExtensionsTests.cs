using System;
using Credfeto.Cache.Proxy.Server.Extensions;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Cache.Proxy.Server.Tests.Extensions;

public sealed class UriExtensionsTests : TestBase
{
    [Fact]
    public void CleanUri_RemovesTrailingSlash()
    {
        Uri uri = new("http://example.com/path/");

        string result = uri.CleanUri();

        Assert.Equal(expected: "http://example.com/path", actual: result);
    }

    [Fact]
    public void CleanUri_LeavesUriWithoutTrailingSlashUnchanged()
    {
        Uri uri = new("http://example.com/path");

        string result = uri.CleanUri();

        Assert.Equal(expected: "http://example.com/path", actual: result);
    }
}
