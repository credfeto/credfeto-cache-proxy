using System;
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

    [Fact]
    public void Constructor_WhenStorageDirectoryDoesNotExist_CreatesDirectory()
    {
        string newStoragePath = Path.Combine(this.TempFolder, "new-storage-dir");
        Assert.False(Directory.Exists(newStoragePath), userMessage: "Directory should not exist before construction");

        _ = new FileSystemPackageStorage(
            Options.Create(new ServerConfig { Sites = [], Storage = newStoragePath }),
            this.GetTypedLogger<FileSystemPackageStorage>()
        );

        Assert.True(
            Directory.Exists(newStoragePath),
            userMessage: "Constructor should have created the missing storage directory"
        );
    }

    [Fact]
    public void ReadFile_WhenBuildPackagePathReturnsFalse_ReturnsNull()
    {
        IPackageStorage storage = new FileSystemPackageStorage(
            Options.Create(new ServerConfig { Sites = [], Storage = string.Empty }),
            this.GetTypedLogger<FileSystemPackageStorage>()
        );

        Stream? result = storage.ReadFile(sourceHost: string.Empty, sourcePath: string.Empty);

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveFileAsync_WhenBuildPackagePathReturnsFalse_DoesNotInvokeCallbackAsync()
    {
        IPackageStorage storage = new FileSystemPackageStorage(
            Options.Create(new ServerConfig { Sites = [], Storage = string.Empty }),
            this.GetTypedLogger<FileSystemPackageStorage>()
        );

        bool callbackInvoked = false;

        await storage.SaveFileAsync(
            sourceHost: string.Empty,
            sourcePath: string.Empty,
            readAsync: (_, _) =>
            {
                callbackInvoked = true;

                return Task.CompletedTask;
            },
            cancellationToken: this.CancellationToken()
        );

        Assert.False(
            callbackInvoked,
            userMessage: "Callback should not be invoked when BuildPackagePath returns false"
        );
    }

    [Fact]
    [System.Runtime.Versioning.SupportedOSPlatform("linux")]
    public void ReadFile_WhenFileCannotBeOpened_ReturnsNull()
    {
        if (!System.OperatingSystem.IsLinux())
        {
            Assert.Skip("Requires Linux file system semantics to test unreadable file");

            return;
        }

        string filePath = Path.Combine(this.TempFolder, HOST, "no-read-perm.txt");
        File.WriteAllText(filePath, contents: "test");

        try
        {
            File.SetUnixFileMode(filePath, UnixFileMode.None);

            Stream? result = this._packageStorage.ReadFile(sourceHost: HOST, sourcePath: "no-read-perm.txt");

            Assert.Null(result);
        }
        finally
        {
            File.SetUnixFileMode(filePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    [Fact]
    public async Task SaveFileAsync_WhenReadAsyncThrows_FileIsNotSavedAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        await this._packageStorage.SaveFileAsync(
            sourceHost: HOST,
            sourcePath: "errored-file.txt",
            readAsync: (_, _) => Task.FromException(new IOException(message: "Simulated failure")),
            cancellationToken: cancellationToken
        );

        Stream? result = this._packageStorage.ReadFile(sourceHost: HOST, sourcePath: "errored-file.txt");
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveFileAsync_WhenFileAlreadyExists_OverwritesFileAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();
        const string SOURCE_PATH = "overwrite-test.txt";

        await this._packageStorage.SaveFileAsync(
            sourceHost: HOST,
            sourcePath: SOURCE_PATH,
            readAsync: async (s, ct) => await s.WriteAsync("initial"u8.ToArray(), ct),
            cancellationToken: cancellationToken
        );

        await this._packageStorage.SaveFileAsync(
            sourceHost: HOST,
            sourcePath: SOURCE_PATH,
            readAsync: async (s, ct) => await s.WriteAsync("updated"u8.ToArray(), ct),
            cancellationToken: cancellationToken
        );

        Stream? result = this._packageStorage.ReadFile(sourceHost: HOST, sourcePath: SOURCE_PATH);
        Assert.NotNull(result);
        Assert.Equal(expected: 7, actual: result.Length);
    }

    [Fact]
    public async Task SaveFileAsync_WhenDirectoryDoesNotExist_CreatesItAndSavesAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();
        const string SOURCE_PATH = "new-subdir/test.txt";

        await this._packageStorage.SaveFileAsync(
            sourceHost: HOST,
            sourcePath: SOURCE_PATH,
            readAsync: async (s, ct) => await s.WriteAsync("data"u8.ToArray(), ct),
            cancellationToken: cancellationToken
        );

        Stream? result = this._packageStorage.ReadFile(sourceHost: HOST, sourcePath: SOURCE_PATH);
        Assert.NotNull(result);
    }

    [Fact]
    [System.Runtime.Versioning.SupportedOSPlatform("linux")]
    public async Task SaveFileAsync_WhenDeleteOfTmpFileThrows_SilentlyIgnoresExceptionAsync()
    {
        if (!System.OperatingSystem.IsLinux())
        {
            Assert.Skip("Requires Linux file system semantics to remove directory write permission");

            return;
        }

        CancellationToken cancellationToken = this.CancellationToken();
        const string SOURCE_PATH = "locked-dir/test.pkg";
        string lockedDir = Path.Combine(this.TempFolder, HOST, "locked-dir");
        Directory.CreateDirectory(lockedDir);
        UnixFileMode originalMode = File.GetUnixFileMode(lockedDir);

        try
        {
            await this._packageStorage.SaveFileAsync(
                sourceHost: HOST,
                sourcePath: SOURCE_PATH,
                readAsync: WriteAndLockAsync,
                cancellationToken: cancellationToken
            );
        }
        finally
        {
            File.SetUnixFileMode(lockedDir, originalMode);
        }

        async Task WriteAndLockAsync(Stream stream, CancellationToken ct)
        {
            await stream.WriteAsync("data"u8.ToArray(), ct);
            File.SetUnixFileMode(lockedDir, UnixFileMode.UserRead | UnixFileMode.UserExecute);
            throw new IOException(message: "Simulated failure after write");
        }
    }
}
