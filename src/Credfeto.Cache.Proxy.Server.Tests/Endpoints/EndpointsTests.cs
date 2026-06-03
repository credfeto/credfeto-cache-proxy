using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Server.Helpers;
using FunFair.Test.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using AppJsonContexts = Credfeto.Cache.Proxy.Models.AppJsonContexts;

namespace Credfeto.Cache.Proxy.Server.Tests.Endpoints;

public sealed class EndpointsTests : LoggingTestBase
{
    public EndpointsTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public async Task PingEndpoint_Returns200WithPongPayloadAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(index: 0, item: AppJsonContexts.Default)
        );

        WebApplication app = builder.Build();

        await using (app)
        {
            _ = app.ConfigureEndpoints();

            await app.StartAsync(cancellationToken);

            TestServer testServer = app.GetTestServer();

            using HttpClient client = testServer.CreateClient();

            using HttpResponseMessage response = await client.GetAsync(
                new Uri("/ping", UriKind.Relative),
                cancellationToken
            );

            Assert.Equal(expected: HttpStatusCode.OK, actual: response.StatusCode);

            string body = await response.Content.ReadAsStringAsync(cancellationToken);

            Assert.Contains("Pong!", body, StringComparison.Ordinal);

            await app.StopAsync(cancellationToken);
        }
    }
}
