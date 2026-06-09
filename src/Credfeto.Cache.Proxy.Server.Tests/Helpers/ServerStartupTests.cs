using Credfeto.Cache.Proxy.Server.Helpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Cache.Proxy.Server.Tests.Helpers;

public sealed class ServerStartupTests : TestBase
{
    [Fact]
    public void SetThreads_DoesNotThrow()
    {
        ServerStartup.SetThreads(minThreads: 1);
    }

    [Fact]
    public void SetThreads_WithHighValue_DoesNotThrow()
    {
        ServerStartup.SetThreads(minThreads: 512);
    }
}
