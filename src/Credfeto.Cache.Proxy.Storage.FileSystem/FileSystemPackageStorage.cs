using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Models.Config;
using Credfeto.Cache.Proxy.Storage.FileSystem.LoggerExtensions;
using Credfeto.Cache.Storage.Interfaces;
using Microsoft.Extensions.Logging;

namespace Credfeto.Cache.Proxy.Storage.FileSystem;

public sealed class FileSystemPackageStorage : IPackageStorage
{
    private readonly ServerConfig _config;
    private readonly ILogger<FileSystemPackageStorage> _logger;

    public FileSystemPackageStorage(ServerConfig config, ILogger<FileSystemPackageStorage> logger)
    {
        this._config = config;
        this._logger = logger;

        this.EnsureDirectoryExists(config.Storage);
    }

    public async ValueTask<byte[]?> ReadFileAsync(string sourceHost, string sourcePath, CancellationToken cancellationToken)
    {
        string packagePath = this.BuildPackagePath(sourceHost: sourceHost, path: sourcePath);

        try
        {
            if (File.Exists(packagePath))
            {
                return await File.ReadAllBytesAsync(path: packagePath, cancellationToken: cancellationToken);
            }
        }
        catch (Exception exception)
        {
            this._logger.FailedToReadFileFromCache(filename: sourcePath, message: exception.Message, exception: exception);

            return null;
        }

        return null;
    }

    public async ValueTask SaveFileAsync(string sourceHost, string sourcePath, byte[] buffer, CancellationToken cancellationToken)
    {
        string packagePath = this.BuildPackagePath(sourceHost: sourceHost, path: sourcePath);

        // ! Doesn't
        string? dir = Path.GetDirectoryName(packagePath);

        if (string.IsNullOrEmpty(dir))
        {
            return;
        }

        try
        {
            this.EnsureDirectoryExists(dir);
            await File.WriteAllBytesAsync(path: packagePath, bytes: buffer, cancellationToken: cancellationToken);
        }
        catch (Exception exception)
        {
            this._logger.SaveFailed(filename: packagePath, message: exception.Message, exception: exception);
        }
    }

    private void EnsureDirectoryExists(string folder)
    {
        try
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }
        catch (Exception exception)
        {
            this._logger.SaveFailed(filename: folder, message: exception.Message, exception: exception);
        }
    }

    private string BuildPackagePath(string sourceHost, string path)
    {
        return Path.Combine(path1: this._config.Storage, path2: sourceHost, path.TrimStart('/'));
    }
}