using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Server.Storage.Services;
using FunFair.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Credfeto.Cache.Proxy.Server.Tests.Storage.Services;

public sealed class HttpClientSetupTests : TestBase
{
    [Fact]
    public void AddContentClient_RegistersHttpClientFactory()
    {
        ServiceCollection services = new();
        services.AddContentClient();

        using ServiceProvider provider = services.BuildServiceProvider();

        IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();

        Assert.NotNull(factory);
    }

    [Fact]
    public void AddContentClient_CreatesConfiguredHttpClient()
    {
        ServiceCollection services = new();
        services.AddContentClient();

        using ServiceProvider provider = services.BuildServiceProvider();

        IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();

        using HttpClient client = factory.CreateClient(nameof(ContentDownloader));

        Assert.NotNull(client);
        Assert.NotEmpty(client.DefaultRequestHeaders.Accept);
    }

    [Fact]
    public async Task AddContentClient_SendsRequestThroughResiliencePipelineAsync()
    {
        byte[] content = [1, 2, 3, 4];

        ServiceCollection services = new();
        services.AddContentClient();
        services
            .AddHttpClient(nameof(ContentDownloader))
            .ConfigurePrimaryHttpMessageHandler(configureHandler: _ => new StubHttpMessageHandler(
                statusCode: HttpStatusCode.OK,
                content: content
            ));

        await using ServiceProvider provider = services.BuildServiceProvider();

        IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();

        using HttpClient client = factory.CreateClient(nameof(ContentDownloader));

        using HttpResponseMessage response = await client.GetAsync(
            requestUri: new Uri("https://upstream.example.com/package"),
            cancellationToken: this.CancellationToken()
        );

        byte[] received = await response.Content.ReadAsByteArrayAsync(this.CancellationToken());

        Assert.Equal(expected: HttpStatusCode.OK, actual: response.StatusCode);
        Assert.Equal(expected: content, actual: received);
    }
}
