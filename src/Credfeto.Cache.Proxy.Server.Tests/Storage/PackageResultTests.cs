using System.IO;
using Credfeto.Cache.Proxy.Models.Config;
using Credfeto.Cache.Proxy.Server.Storage;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Cache.Proxy.Server.Tests.Storage;

public sealed class PackageResultTests : TestBase
{
    [Fact]
    public void Properties_DataAndCacheSettingAreSet()
    {
        byte[] bytes = [1, 2, 3];
        MemoryStream stream = new(bytes);
        CacheSetting cacheSetting = new() { Match = ".*", LifeTimeSeconds = 3600 };

        PackageResult result = new(Data: stream, CacheSetting: cacheSetting);

        Assert.Same(expected: stream, actual: result.Data);
        Assert.Same(expected: cacheSetting, actual: result.CacheSetting);
    }

    [Fact]
    public void Equality_EqualForSameDataAndCacheSetting()
    {
        byte[] bytes = [1, 2, 3];
        MemoryStream stream = new(bytes);
        CacheSetting cacheSetting = new() { Match = ".*", LifeTimeSeconds = 3600 };

        PackageResult first = new(Data: stream, CacheSetting: cacheSetting);
        PackageResult second = new(Data: stream, CacheSetting: cacheSetting);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Equality_NotEqualForDifferentData()
    {
        byte[] bytes1 = [1, 2, 3];
        byte[] bytes2 = [4, 5, 6];
        MemoryStream stream1 = new(bytes1);
        MemoryStream stream2 = new(bytes2);
        CacheSetting cacheSetting = new() { Match = ".*", LifeTimeSeconds = 3600 };

        PackageResult first = new(Data: stream1, CacheSetting: cacheSetting);
        PackageResult second = new(Data: stream2, CacheSetting: cacheSetting);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Properties_NullCacheSettingIsAllowed()
    {
        byte[] bytes = [1, 2, 3];
        MemoryStream stream = new(bytes);

        PackageResult result = new(Data: stream, CacheSetting: null);

        Assert.Same(expected: stream, actual: result.Data);
        Assert.Null(result.CacheSetting);
    }
}
