using System.Diagnostics;

namespace Credfeto.Cache.Proxy.Models.Config;

[DebuggerDisplay("Match: {Match} Lifetime: {LifeTimeSeconds}s")]
public sealed class CacheSetting
{
    public CacheSetting()
    {
        this.LifeTimeSeconds = 63115200;
        this.Match = "^$";
    }

    public int LifeTimeSeconds { get; set; }

    public string Match { get; set; }
}