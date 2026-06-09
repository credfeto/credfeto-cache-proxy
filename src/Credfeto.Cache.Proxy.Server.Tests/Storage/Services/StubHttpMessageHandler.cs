using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.Cache.Proxy.Server.Tests.Storage.Services;

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly byte[] _content;
    private readonly HttpStatusCode _statusCode;

    public StubHttpMessageHandler(HttpStatusCode statusCode, byte[] content)
    {
        this._statusCode = statusCode;
        this._content = content;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        HttpResponseMessage response = new(this._statusCode) { Content = new ByteArrayContent(this._content) };

        return Task.FromResult(response);
    }
}
