using Credfeto.Cache.Proxy.Models.Config;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Cache.Proxy.Models.Tests.Config;

public sealed class CacheSettingTests : TestBase
{
    [Fact]
    public void ConstructorSetsDefaultLifeTimeSeconds()
    {
        CacheSetting setting = new();

        Assert.Equal(expected: 63115200, actual: setting.LifeTimeSeconds);
    }

    [Fact]
    public void ConstructorSetsDefaultMatch()
    {
        CacheSetting setting = new();

        Assert.Equal(expected: "^$", actual: setting.Match);
    }
}
