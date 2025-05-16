using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Credfeto.Cache.Proxy.Models.Config;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Credfeto.Cache.Proxy.Server.Storage.Services;

internal static class HttpClientSetup
{
    private const int CONCURRENT_ACTIONS = 30;
    private const int QUEUED_ACTIONS = 10;
    private static readonly TimeSpan HttpTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan HandlerTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan PollyTimeout = HttpTimeout.Add(TimeSpan.FromSeconds(1));

    public static IServiceCollection AddContentClient(this IServiceCollection services, ServerConfig appConfig)
    {
        foreach (CacheServerConfig config in appConfig.Sites)
        {
            Uri target = HttpClientNames.GetHttpClientUri(config: config, out string name);

            services = services
                .AddHttpClient(
                    name: name,
                    configureClient: httpClient =>
                        InitializeContentClient(upstreamUrl: target, httpClient: httpClient, httpTimeout: HttpTimeout)
                )
                .SetHandlerLifetime(HandlerTimeout)
                .ConfigurePrimaryHttpMessageHandler(configureHandler: _ => new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.All,
                })
                .AddPolicyHandler(Policy.BulkheadAsync<HttpResponseMessage>(CONCURRENT_ACTIONS * 2, QUEUED_ACTIONS * 2))
                .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(PollyTimeout))
                .Services;
        }

        return services;
    }

    private static void InitializeContentClient(Uri upstreamUrl, HttpClient httpClient, in TimeSpan httpTimeout)
    {
        httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
        httpClient.DefaultRequestVersion = HttpVersion.Version11;
        httpClient.BaseAddress = upstreamUrl;
        httpClient.DefaultRequestHeaders.Accept.Add(new(mediaType: "application/octet-stream"));
        httpClient.DefaultRequestHeaders.UserAgent.Add(
            new(new ProductHeaderValue(name: VersionInformation.Product, version: VersionInformation.Version))
        );
        httpClient.Timeout = httpTimeout;
    }
}
