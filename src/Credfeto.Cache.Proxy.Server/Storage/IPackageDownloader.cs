using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Server.Config;
using Microsoft.AspNetCore.Http;

namespace Credfeto.Cache.Proxy.Server.Storage;

public interface IPackageDownloader
{
    ValueTask<byte[]> ReadUpstreamAsync(
        CacheServerConfig config,
        PathString path,
        ProductInfoHeaderValue? userAgent,
        CancellationToken cancellationToken
    );
}
