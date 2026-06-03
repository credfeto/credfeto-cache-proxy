using System.Net.Http;
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
}
