using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Credfeto.Docker.HealthCheck.Http.Client;

namespace Credfeto.Cache.Proxy.Server.Helpers;

public static class CommandDispatcher
{
    public static ValueTask<int> DispatchAsync(
        IReadOnlyList<string> args,
        Func<string, ValueTask<int>> healthCheck,
        Func<ValueTask<int>> selfTest,
        Func<ValueTask<int>> runServer
    )
    {
        if (HealthCheckClient.IsHealthCheck(args: [.. args], out string? checkUrl))
        {
            return healthCheck(checkUrl);
        }

        return SelfTest.IsSelfTest(args) ? selfTest() : runServer();
    }
}
