using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Models.Config;
using Microsoft.AspNetCore.Http;

namespace Credfeto.Cache.Proxy.Server.Storage;

public interface IContentDownloader
{
    ValueTask<Stream> ReadUpstreamAsync(
        CacheServerConfig config,
        PathString path,
        ProductInfoHeaderValue? userAgent,
        CancellationToken cancellationToken
    );
}
