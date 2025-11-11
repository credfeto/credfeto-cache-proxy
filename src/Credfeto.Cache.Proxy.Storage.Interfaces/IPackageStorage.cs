using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.Cache.Proxy.Storage.Interfaces;

public interface IPackageStorage
{
    Stream? ReadFile(string sourceHost, string sourcePath);

    [SuppressMessage(category: "Meziantou.Analyzer", checkId: "MA0109: Add an overload with a Span or Memory parameter", Justification = "Won't work here")]
    ValueTask SaveFileAsync(string sourceHost, string sourcePath, Func<Stream, CancellationToken, Task> readAsync, CancellationToken cancellationToken);
}