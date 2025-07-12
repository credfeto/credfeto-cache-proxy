using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Credfeto.Cache.Proxy.Models.Config;

[DebuggerDisplay("Source: {Source} Target: {Target}")]
public sealed class CacheServerConfig
{
    public CacheServerConfig()
    {
        this.Source = "localhost";
        this.Target = "example.com";
        this.Settings = [];
    }

    public string Source { get; set; }

    public string Target { get; set; }

    public List<CacheSetting> Settings { get; set; }

    public string HostOnlyTarget()
    {
        Uri uri = new(uriString: this.Target, uriKind: UriKind.Absolute);

        return uri.DnsSafeHost;
    }
}