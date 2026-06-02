using Credfeto.Cache.Proxy.Server.Helpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Cache.Proxy.Server.Tests.Helpers;

public sealed class ApplicationConfigLocatorTests : TestBase
{
    [Fact]
    public void ConfigurationFilesPath_ReturnsNonEmptyString()
    {
        string path = ApplicationConfigLocator.ConfigurationFilesPath;

        Assert.NotEmpty(path);
    }
}
