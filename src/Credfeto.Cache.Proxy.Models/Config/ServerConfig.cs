using System.Collections.Generic;
using System.Diagnostics;

namespace Credfeto.Cache.Proxy.Models.Config;

[DebuggerDisplay("Storage: {Storage} Sites: {Sites.Count} ")]
public sealed class ServerConfig
{
    public ServerConfig()
    {
        this.Sites = [];
        this.Storage = "/data";
    }

    public List<CacheServerConfig> Sites { get; set; }

    public string Storage { get; set; }
}