using Credfeto.Cache.Proxy.Models.Config;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Cache.Proxy.Models.Tests.Config;

public sealed class ServerConfigTests : TestBase
{
    [Fact]
    public void ConstructorSetsEmptySites()
    {
        ServerConfig config = new();

        Assert.Empty(config.Sites);
    }

    [Fact]
    public void ConstructorSetsDefaultStorage()
    {
        ServerConfig config = new();

        Assert.Equal(expected: "/data", actual: config.Storage);
    }
}
