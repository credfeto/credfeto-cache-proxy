using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Server.Helpers;
using Credfeto.Cache.Proxy.Server.Middleware;
using Credfeto.Docker.HealthCheck.Http.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging.Abstractions;

namespace Credfeto.Cache.Proxy.Server;

public static class Program
{
    private const int MIN_THREADS = 32;

    [SuppressMessage(
        category: "Meziantou.Analyzer",
        checkId: "MA0109: Add an overload with a Span or Memory parameter",
        Justification = "Won't work here"
    )]
    public static async Task<int> Main(string[] args)
    {
        return await CommandDispatcher.DispatchAsync(
            args: args,
            healthCheck: CheckHealthAsync,
            selfTest: RunSelfTestAsync,
            runServer: () => RunServerAsync(args)
        );
    }

    private static ValueTask<int> CheckHealthAsync(string checkUrl)
    {
        return HealthCheckClient.ExecuteAsync(
            targetUrl: checkUrl,
            logger: NullLogger.Instance,
            cancellationToken: CancellationToken.None
        );
    }

    private static ValueTask<int> RunSelfTestAsync()
    {
        return SelfTest.RunAsync(
            configPath: ApplicationConfigLocator.ConfigurationFilesPath,
            output: Console.Out,
            cancellationToken: CancellationToken.None
        );
    }

    private static async ValueTask<int> RunServerAsync(string[] args)
    {
        StartupBanner.Show();

        ServerStartup.SetThreads(MIN_THREADS);

        try
        {
            await using (
                WebApplication app = ServerStartup.CreateApp(
                    args: args,
                    configPath: ApplicationConfigLocator.ConfigurationFilesPath
                )
            )
            {
                await RunAsync(app);

                return 0;
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine("An error occurred:");
            Console.WriteLine(exception.Message);
            Console.WriteLine(exception.StackTrace);

            return 1;
        }
    }

    private static Task RunAsync(WebApplication application)
    {
        Console.WriteLine("App Created");

        return AddMiddleware(application).RunAsync();
    }

    private static WebApplication AddMiddleware(WebApplication application)
    {
        return (WebApplication)
            ((WebApplication)application.UseForwardedHeaders().UseMiddleware<ServerHeaderMiddleware>())
                .ConfigureEndpoints()
                .UseMiddleware<CacheMiddleware>();
    }
}
