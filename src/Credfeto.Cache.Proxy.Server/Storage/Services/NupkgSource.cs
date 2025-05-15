using System;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Models.Config;
using Credfeto.Cache.Storage.Interfaces;

namespace Credfeto.Cache.Proxy.Server.Storage.Services;

public sealed class NupkgSource : INupkgSource
{
    private readonly IPackageDownloader _packageDownloader;
    private readonly IPackageStorage _packageStorage;

    public NupkgSource(IPackageStorage packageStorage, IPackageDownloader packageDownloader)
    {
        this._packageStorage = packageStorage;
        this._packageDownloader = packageDownloader;
    }

    public async ValueTask<PackageResult?> GetFromUpstreamAsync(
        CacheServerConfig config,
        string path,
        ProductInfoHeaderValue? userAgent,
        CancellationToken cancellationToken
    )
    {
        return await this.TryToGetFromCacheAsync(config: config, sourcePath: path, cancellationToken: cancellationToken)
            ?? await this.GetFromUpstream2Async(
                config: config,
                sourcePath: path,
                userAgent: userAgent,
                cancellationToken: cancellationToken
            );
    }

    private async ValueTask<PackageResult?> TryToGetFromCacheAsync(
        CacheServerConfig config,
        string sourcePath,
        CancellationToken cancellationToken
    )
    {
        byte[]? data = await this._packageStorage.ReadFileAsync(
            Path.Combine(path1: config.Target, path2: sourcePath),
            cancellationToken: cancellationToken
        );

        return data is null ? null : new(Data: data, ShouldCache(config: config, sourcePath: sourcePath));
    }

    private async ValueTask<PackageResult?> GetFromUpstream2Async(
        CacheServerConfig config,
        string sourcePath,
        ProductInfoHeaderValue? userAgent,
        CancellationToken cancellationToken
    )
    {
        byte[] data = await this._packageDownloader.ReadUpstreamAsync(
            config: config,
            path: sourcePath,
            userAgent: userAgent,
            cancellationToken: cancellationToken
        );

        CacheSetting? cacheSetting = ShouldCache(config: config, sourcePath: sourcePath);

        if (cacheSetting is not null)
        {
            await this._packageStorage.SaveFileAsync(
                sourcePath: sourcePath,
                buffer: data,
                cancellationToken: cancellationToken
            );
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
