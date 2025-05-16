using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Models.Config;

namespace Credfeto.Cache.Proxy.Server.Storage;

public interface IContentSource
{
    ValueTask<PackageResult?> GetFromUpstreamAsync(CacheServerConfig config, string path, ProductInfoHeaderValue? userAgent, CancellationToken cancellationToken);
}