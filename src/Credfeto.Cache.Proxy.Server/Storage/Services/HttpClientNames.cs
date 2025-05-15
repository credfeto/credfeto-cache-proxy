using System;
using Credfeto.Cache.Proxy.Server.Config;

namespace Credfeto.Cache.Proxy.Server.Storage.Services;

public static class HttpClientNames
{
    private const string NUGET_PACKAGE = "NugetPackageClient";

    public static Uri GetHttpClientUri(CacheServerConfig config, out string name)
    {
        Uri target = new(config.Target);

        name = NUGET_PACKAGE + "-" + target.DnsSafeHost;

        return target;
    }
}
