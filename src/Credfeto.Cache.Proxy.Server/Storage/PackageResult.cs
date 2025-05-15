using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Credfeto.Cache.Proxy.Server.Config;

namespace Credfeto.Cache.Proxy.Server.Storage;

[SuppressMessage(category: "Meziantou.Analyzer", checkId: "MA0109: Add an overload with a Span or Memory parameter", Justification = "Won't work here")]
[DebuggerDisplay("Length: {Data.Length} bytes")]
public readonly record struct PackageResult(byte[] Data, CacheSetting? CacheSetting);