using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Models.Config;
using Credfeto.Cache.Storage.Interfaces;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Cache.Proxy.Storage.FileSystem.Tests;

public sealed class FileSystemPackageStorageTests : LoggingFolderCleanupTestBase
{
    private const string HOST = "example.com";

    private readonly IPackageStorage _packageStorage;

    public FileSystemPackageStorageTests(ITestOutputHelper output)
        : base(output)
    {
        ServerConfig config = new([], Storage: this.TempFolder);

        this._packageStorage = new FileSystemPackageStorage(config: config, this.GetTypedLogger<FileSystemPackageStorage>());
    }

    [Fact]
    public async Task FileDoesNotExistAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        byte[]? result = await this._packageStorage.ReadFileAsync(sourceHost: HOST, sourcePath: "doesnotexist", cancellationToken: cancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task FileExistsAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        await File.WriteAllTextAsync(Path.Combine(path1: this.TempFolder, path2: HOST, path3: "file.txt"), contents: "test", cancellationToken: cancellationToken);

        byte[]? result = await this._packageStorage.ReadFileAsync(sourceHost: HOST, sourcePath: "file.txt", cancellationToken: cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(expected: 4, actual: result.Length);
    }
}