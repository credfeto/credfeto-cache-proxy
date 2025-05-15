using System.Diagnostics;

namespace Credfeto.Cache.Proxy.Models.Config;

[DebuggerDisplay("Match: {Match} Lifetime: {LifeTimeSeconds}s")]
public sealed record CacheSetting(string Match, int LifeTimeSeconds);
