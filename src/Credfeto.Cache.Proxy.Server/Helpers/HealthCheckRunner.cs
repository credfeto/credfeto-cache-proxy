using System.Threading;
using System.Threading.Tasks;
using Credfeto.Docker.HealthCheck.Http.Client;
using Microsoft.Extensions.Logging;

namespace Credfeto.Cache.Proxy.Server.Helpers;

public static class HealthCheckRunner
{
    public static async ValueTask<int> RunAsync(string checkUrl, CancellationToken cancellationToken)
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(static builder => builder.AddConsole());

        return await HealthCheckClient.ExecuteAsync(
            targetUrl: checkUrl,
            logger: loggerFactory.CreateLogger(nameof(HealthCheckClient)),
            cancellationToken: cancellationToken
        );
    }
}
