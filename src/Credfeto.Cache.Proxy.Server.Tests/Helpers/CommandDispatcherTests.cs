using System.Collections.Generic;
using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Server.Helpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Cache.Proxy.Server.Tests.Helpers;

public sealed class CommandDispatcherTests : TestBase
{
    private const int HEALTH_CHECK_RESULT = 11;
    private const int SELF_TEST_RESULT = 22;
    private const int RUN_SERVER_RESULT = 33;
    private const string HEALTH_CHECK_URL = "http://127.0.0.1:8080/ping?source=docker";

    private readonly List<string> _healthCheckUrls = [];

    [Fact]
    public async Task DispatchAsync_WhenHealthCheck_RunsHealthCheckWithUrlAsync()
    {
        int result = await this.DispatchAsync(["--health-check", HEALTH_CHECK_URL]);

        Assert.Equal(expected: HEALTH_CHECK_RESULT, actual: result);
        string checkUrl = Assert.Single(this._healthCheckUrls);
        Assert.Equal(expected: HEALTH_CHECK_URL, actual: checkUrl);
    }

    [Fact]
    public async Task DispatchAsync_WhenSelfTest_RunsSelfTestAsync()
    {
        int result = await this.DispatchAsync(["--self-test"]);

        Assert.Equal(expected: SELF_TEST_RESULT, actual: result);
        Assert.Empty(this._healthCheckUrls);
    }

    [Fact]
    public async Task DispatchAsync_WhenNoSwitches_RunsServerAsync()
    {
        int result = await this.DispatchAsync([]);

        Assert.Equal(expected: RUN_SERVER_RESULT, actual: result);
        Assert.Empty(this._healthCheckUrls);
    }

    private ValueTask<int> DispatchAsync(string[] args)
    {
        return CommandDispatcher.DispatchAsync(
            args: args,
            healthCheck: this.HealthCheckAsync,
            selfTest: static () => ValueTask.FromResult(SELF_TEST_RESULT),
            runServer: static () => ValueTask.FromResult(RUN_SERVER_RESULT)
        );
    }

    private ValueTask<int> HealthCheckAsync(string checkUrl)
    {
        this._healthCheckUrls.Add(checkUrl);

        return ValueTask.FromResult(HEALTH_CHECK_RESULT);
    }
}
