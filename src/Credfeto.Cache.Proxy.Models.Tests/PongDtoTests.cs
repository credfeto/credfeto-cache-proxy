using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Cache.Proxy.Models.Tests;

public sealed class PongDtoTests : TestBase
{
    [Fact]
    public void ConstructorSetsValue()
    {
        PongDto dto = new(value: "pong");

        Assert.Equal(expected: "pong", actual: dto.Value);
    }

    [Fact]
    public void EqualityHoldsForSameValue()
    {
        PongDto first = new(value: "pong");
        PongDto second = new(value: "pong");

        Assert.Equal(first, second);
    }

    [Fact]
    public void InequalityHoldsForDifferentValues()
    {
        PongDto first = new(value: "pong");
        PongDto second = new(value: "other");

        Assert.NotEqual(first, second);
    }
}
