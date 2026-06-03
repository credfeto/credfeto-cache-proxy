using Credfeto.Cache.Proxy.Server.Helpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Cache.Proxy.Server.Tests.Helpers;

public sealed class PingPongTests : TestBase
{
    [Fact]
    public void Model_ValueEqualsPong()
    {
        Assert.Equal(expected: "Pong!", actual: PingPong.Model.Value);
    }
}
