using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Models.Config;
using Credfeto.Cache.Proxy.Storage.FileSystem.LoggerExtensions;
using Credfeto.Cache.Proxy.Storage.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Credfeto.Cache.Proxy.Storage.FileSystem;

public sealed class FileSystemPackageStorage : IPackageStorage
{
    private readonly ServerConfig _config;
    private readonly ILogger<FileSystemPackageStorage> _logger;

    public FileSystemPackageStorage(IOptions<ServerConfig> config, ILogger<FileSystemPackageStorage> logger)
    {
        this._config = config.Value;
        this._logger = logger;

        this.EnsureDirectoryExists(this._config.Storage);
    }

    public Stream? ReadFile(
        string sourceHost,
        string sourcePath
    )
    {
        if (!this.BuildPackagePath(sourceHost: sourceHost, path: sourcePath, out string? packagePath, out string? dir))
        {
            return null;
        }

        try
        {
            if (Directory.Exists(dir) && File.Exists(packagePath))
            {
                return File.OpenRead(path: packagePath);
            }
        }
        catch (Exception exception)
        {
            this._logger.FailedToReadFileFromCache(
                filename: sourcePath,
                message: exception.Message,
                exception: exception
            );

            return null;
        }

        return null;
    }

    public async ValueTask SaveFileAsync(
        string sourceHost,
        string sourcePath,
        Stream buffer,
        CancellationToken cancellationToken
    )
    {
        if (!this.BuildPackagePath(sourceHost: sourceHost, path: sourcePath, out string? packagePath, out string? dir))
        {
            return;
        }

        string tmpPath = string.Join(".",
                                     packagePath,
                                     Guid.NewGuid()
                                         .ToString(),
                                     "tmp");

        try
        {
            this.EnsureDirectoryExists(dir);

            await using (FileStream stream = File.OpenWrite(tmpPath))
            {
                await buffer.CopyToAsync(stream, cancellationToken);
            }

            DeleteFile(packagePath);
            File.Move(tmpPath, packagePath);
        }
        catch (Exception exception)
        {
            this._logger.SaveFailed(filename: packagePath, message: exception.Message, exception: exception);

            DeleteFile(tmpPath);
        }
    }

    private static void DeleteFile(string fileName)
    {
        if (!File.Exists(fileName))
        {
            return;
        }

        try
        {
            File.Delete(fileName);
        }
        catch (Exception exception)
        {
            Debug.Write(exception.Message);
        }
    }

    private void EnsureDirectoryExists(string directory)
    {
        try
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        catch (Exception exception)
        {
            this._logger.SaveFailed(filename: directory, message: exception.Message, exception: exception);
        }
    }

    private bool BuildPackagePath(
        string sourceHost,
        string path,
        [NotNullWhen(true)] out string? filename,
        [NotNullWhen(true)] out string? dir
    )
    {
        string f = Path.Combine(path1: this._config.Storage, path2: sourceHost, path.TrimStart('/'));

        string? d = Path.GetDirectoryName(f);

        if (string.IsNullOrEmpty(d))
        {
            filename = null;
            dir = null;

            return false;
        }

        filename = f;
        dir = d;

        return true;
    }
}
