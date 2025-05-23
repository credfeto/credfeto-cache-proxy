using System.Collections.Generic;
using System.Diagnostics;

namespace Credfeto.Cache.Proxy.Models.Config;

[DebuggerDisplay("Storage: {Storage} Sites: {Sites.Count} ")]
public sealed record ServerConfig(IReadOnlyList<CacheServerConfig> Sites, string Storage);
