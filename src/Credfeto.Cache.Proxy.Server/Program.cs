using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Server.Helpers;
using Credfeto.Cache.Proxy.Server.Middleware;
using Credfeto.Docker.HealthCheck.Http.Client;
using Microsoft.AspNetCore.Builder;

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
        return HealthCheckClient.IsHealthCheck(args: args, out string? checkUrl)
            ? await HealthCheckClient.ExecuteAsync(targetUrl: checkUrl, cancellationToken: CancellationToken.None)
            : await RunServerAsync(args);
    }

    private static async ValueTask<int> RunServerAsync(string[] args)
    {
        StartupBanner.Show();

        ServerStartup.SetThreads(MIN_THREADS);

        try
        {
            await using (WebApplication app = ServerStartup.CreateApp(args))
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
        WebApplication app = (WebApplication)application.UseForwardedHeaders();

        return (WebApplication)app.ConfigureEndpoints().UseMiddleware<CacheMiddleware>();
    }
}
