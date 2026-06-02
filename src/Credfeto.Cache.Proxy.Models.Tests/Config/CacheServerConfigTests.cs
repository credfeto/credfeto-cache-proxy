using Credfeto.Cache.Proxy.Models.Config;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Cache.Proxy.Models.Tests.Config;

public sealed class CacheServerConfigTests : TestBase
{
    [Fact]
    public void ConstructorSetsDefaultSource()
    {
        CacheServerConfig config = new();

        Assert.Equal(expected: "localhost", actual: config.Source);
    }

    [Fact]
    public void ConstructorSetsDefaultTarget()
    {
        CacheServerConfig config = new();

        Assert.Equal(expected: "example.com", actual: config.Target);
    }

    [Fact]
    public void ConstructorSetsEmptySettings()
    {
        CacheServerConfig config = new();

        Assert.Empty(config.Settings);
    }

    [Theory]
    [InlineData("http://example.com", "example.com")]
    [InlineData("http://example.com/some/path", "example.com")]
    [InlineData("https://my-server.internal/api/v1", "my-server.internal")]
    public void HostOnlyTargetReturnsDnsHost(string target, string expectedHost)
    {
        CacheServerConfig config = new() { Target = target };

        string actual = config.HostOnlyTarget();

        Assert.Equal(expected: expectedHost, actual: actual);
    }
}
