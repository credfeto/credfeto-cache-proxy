using System;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Models.Config;
using Credfeto.Cache.Proxy.Storage.Interfaces;

namespace Credfeto.Cache.Proxy.Server.Storage.Services;

public sealed class ContentSource : IContentSource
{
    private static readonly CancellationToken DoNotCancelEarly = CancellationToken.None;
    private readonly IContentDownloader _contentDownloader;
    private readonly IPackageStorage _packageStorage;

    public ContentSource(IPackageStorage packageStorage, IContentDownloader contentDownloader)
    {
        this._packageStorage = packageStorage;
        this._contentDownloader = contentDownloader;
    }

    public async ValueTask<PackageResult?> GetFromUpstreamAsync(
        CacheServerConfig config,
        string path,
        ProductInfoHeaderValue? userAgent,
        CancellationToken cancellationToken
    )
    {

        return  this.TryToGetFromCache(config: config, sourcePath: path)
            ?? await this.GetFromUpstream2Async(
                config: config,
                sourcePath: path,
                userAgent: userAgent,
                cancellationToken: cancellationToken
            );
    }

    private PackageResult? TryToGetFromCache(
        CacheServerConfig config,
        string sourcePath
    )
    {
        if (RequestHasQuery(sourcePath))
        {
            return null;
        }

        string host = config.HostOnlyTarget();

        Stream? data = this._packageStorage.ReadFile(sourceHost: host, Path.Combine(path1: config.Target, path2: sourcePath));

        return data is null ? null : new(Data: data, ShouldCache(config: config, sourcePath: sourcePath));
    }

    private static bool RequestHasQuery(string sourcePath)
    {
        return sourcePath.Contains(value: '?', comparisonType: StringComparison.Ordinal);
    }

    private async ValueTask<PackageResult?> GetFromUpstream2Async(
        CacheServerConfig config,
        string sourcePath,
        ProductInfoHeaderValue? userAgent,
        CancellationToken cancellationToken
    )
    {
        Stream data = await this._contentDownloader.ReadUpstreamAsync(
            config: config,
            path: sourcePath,
            userAgent: userAgent,
            cancellationToken: cancellationToken
        );

        CacheSetting? cacheSetting = ShouldCache(config: config, sourcePath: sourcePath);

        if (cacheSetting is not null && !RequestHasQuery(sourcePath))
        {
            string host = config.HostOnlyTarget();
            await this._packageStorage.SaveFileAsync(
                sourceHost: host,
                sourcePath: sourcePath,
                buffer: data,
                cancellationToken: DoNotCancelEarly
            );

            Stream? stream = this._packageStorage.ReadFile(host, sourcePath);

            return stream is null ? null : new PackageResult(Data: stream, CacheSetting: cacheSetting);
        }

        return new PackageResult(Data: data, CacheSetting: cacheSetting);
    }

    private static CacheSetting? ShouldCache(CacheServerConfig config, string sourcePath)
    {
        return config.Settings.FirstOrDefault(setting => IsMatchingPath(setting: setting, sourcePath: sourcePath));
    }

    private static bool IsMatchingPath(CacheSetting setting, string sourcePath)
    {
        return Regex.IsMatch(
            input: sourcePath,
            pattern: setting.Match,
            RegexOptions.Compiled
                | RegexOptions.ExplicitCapture
                | RegexOptions.NonBacktracking
                | RegexOptions.Singleline,
            TimeSpan.FromSeconds(0.5)
        );
    }
}
