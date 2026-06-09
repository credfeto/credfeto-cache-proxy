using Credfeto.Cache.Proxy.Server.Helpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Cache.Proxy.Server.Tests.Helpers;

public sealed class StartupBannerTests : TestBase
{
    [Fact]
    public void Show_DoesNotThrow()
    {
        StartupBanner.Show();
    }
}
