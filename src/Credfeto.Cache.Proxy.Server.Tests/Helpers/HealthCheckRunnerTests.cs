using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Server.Helpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Cache.Proxy.Server.Tests.Helpers;

public sealed class HealthCheckRunnerTests : TestBase
{
    private const string UNREACHABLE_URL = "http://127.0.0.1:1/ping?source=docker";

    [Fact]
    public async Task RunAsync_WhenServerUnreachable_ReturnsFailureAsync()
    {
        int result = await HealthCheckRunner.RunAsync(
            checkUrl: UNREACHABLE_URL,
            cancellationToken: this.CancellationToken()
        );

        Assert.NotEqual(expected: 0, actual: result);
    }
}
