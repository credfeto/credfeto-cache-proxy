using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Credfeto.Cache.Proxy.Models.Config;

[DebuggerDisplay("Source: {Source} Target: {Target}")]
public sealed record CacheServerConfig(string Source, string Target, IReadOnlyList<CacheSetting> Settings)
{
    public string HostOnlyTarget()
    {
        Uri uri = new(uriString: this.Target, uriKind: UriKind.Absolute);

        return uri.DnsSafeHost;
    }
}