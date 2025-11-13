using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Models.Config;
using Credfeto.Cache.Proxy.Storage.Interfaces;
using FunFair.Test.Common;
using Microsoft.Extensions.Options;
using Xunit;

namespace Credfeto.Cache.Proxy.Storage.FileSystem.Tests;

public sealed class FileSystemPackageStorageTests : LoggingFolderCleanupTestBase
{
    private const string HOST = "example.com";

    private readonly IPackageStorage _packageStorage;

    public FileSystemPackageStorageTests(ITestOutputHelper output)
        : base(output)
    {
        ServerConfig config = new() { Sites = [], Storage = this.TempFolder };

        string directory = Path.Combine(path1: this.TempFolder, path2: HOST);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        this._packageStorage = new FileSystemPackageStorage(
            Options.Create(config),
            this.GetTypedLogger<FileSystemPackageStorage>()
        );
    }

    [Fact]
    public void FileDoesNotExist()
    {
        Stream? result = this._packageStorage.ReadFile(sourceHost: HOST, sourcePath: "doesnotexist");

        Assert.Null(result);
    }

    [Fact]
    public async Task FileExistsAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        await File.WriteAllTextAsync(
            Path.Combine(path1: this.TempFolder, path2: HOST, path3: "file.txt"),
            contents: "test",
            cancellationToken: cancellationToken
        );

        Stream? result = this._packageStorage.ReadFile(sourceHost: HOST, sourcePath: "file.txt");

        Assert.NotNull(result);
        Assert.Equal(expected: 4, actual: result.Length);
    }

    [Fact]
    public async Task SafeFileMakeItExistAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        Stream? resultBefore = this._packageStorage.ReadFile(sourceHost: HOST, sourcePath: "file.txt");

        Assert.Null(resultBefore);

        await this._packageStorage.SaveFileAsync(
            sourceHost: HOST,
            sourcePath: "file.txt",
            async (s, ct) =>
            {
                await s.WriteAsync("test"u8.ToArray(), ct);
            },
            cancellationToken: cancellationToken
        );

        Stream? resultAfter = this._packageStorage.ReadFile(sourceHost: HOST, sourcePath: "file.txt");

        Assert.NotNull(resultAfter);
        Assert.Equal(expected: 4, actual: resultAfter.Length);
    }
}
