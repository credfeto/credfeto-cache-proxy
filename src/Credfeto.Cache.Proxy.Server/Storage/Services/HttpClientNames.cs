using System;
using Credfeto.Cache.Proxy.Models.Config;

namespace Credfeto.Cache.Proxy.Server.Storage.Services;

public static class HttpClientNames
{
    private const string CLIENT_PREFIX = "ContentClient";

    public static Uri GetHttpClientUri(CacheServerConfig config, out string name)
    {
        Uri target = new(config.Target);

        name = CLIENT_PREFIX + "-" + target.DnsSafeHost;

        return target;
    }
}
