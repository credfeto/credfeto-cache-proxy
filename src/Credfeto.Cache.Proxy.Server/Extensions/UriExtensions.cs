using System;

namespace Credfeto.Cache.Proxy.Server.Extensions;

public static class UriExtensions
{
    public static string CleanUri(this Uri uri)
    {
        return uri.ToString().TrimEnd('/');
    }
}
