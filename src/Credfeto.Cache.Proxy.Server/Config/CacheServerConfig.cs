using System.Collections.Generic;
using System.Diagnostics;

namespace Credfeto.Cache.Proxy.Server.Config;

[DebuggerDisplay("Source: {Source} Target: {Target}")]
public sealed record CacheServerConfig(string Source, string Target, IReadOnlyList<CacheSetting> Settings);