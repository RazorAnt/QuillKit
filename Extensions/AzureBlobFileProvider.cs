using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Azure.Storage.Blobs;
using QuillKit.Services;

namespace QuillKit.Extensions;

/// <summary>
/// 🌐 File provider that serves files from Azure Blob Storage
/// Specifically designed for serving theme views and other content files
/// </summary>
public class AzureBlobFileProvider : IFileProvider
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly ILogger<AzureBlobFileProvider> _logger;

    public AzureBlobFileProvider(IConfiguration configuration, ILogger<AzureBlobFileProvider> logger)
    {
        _logger = logger;
        _containerName = "content";

        var connectionString = configuration.GetConnectionString("AzureStorage");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Azure Storage connection string is required for blob file provider");
        }

        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        if (string.IsNullOrEmpty(subpath) || subpath == "/")
        {
            return new NotFoundFileInfo(subpath);
        }

        // Remove leading slash and convert to blob path
        var blobPath = subpath.TrimStart('/');
        
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobPath);

            return new AzureBlobFileInfo(blobClient, subpath, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting file info for: {SubPath}", subpath);
            return new NotFoundFileInfo(subpath);
        }
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        // For now, return empty directory contents
        // This could be implemented later if needed for directory browsing
        return NotFoundDirectoryContents.Singleton;
    }

    public IChangeToken Watch(string filter)
    {
        // Azure Blob doesn't support file watching in the same way as local files
        // Return a null change token to indicate no change notifications
        return NullChangeToken.Singleton;
    }
}

/// <summary>
/// 📄 File info implementation for Azure Blob Storage files
/// </summary>
public class AzureBlobFileInfo : IFileInfo
{
    private readonly BlobClient _blobClient;
    private readonly string _subpath;
    private readonly ILogger _logger;
    private readonly Lazy<bool> _exists;
    private readonly Lazy<long> _length;
    private readonly Lazy<DateTimeOffset> _lastModified;

    public AzureBlobFileInfo(BlobClient blobClient, string subpath, ILogger logger)
    {
        _blobClient = blobClient;
        _subpath = subpath;
        _logger = logger;
        
        _exists = new Lazy<bool>(() =>
        {
            try
            {
                return _blobClient.Exists().Value;
            }
            catch
            {
                return false;
            }
        });

        _length = new Lazy<long>(() =>
        {
            try
            {
                if (_exists.Value)
                {
                    var properties = _blobClient.GetProperties().Value;
                    return properties.ContentLength;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting blob length: {BlobName}", _blobClient.Name);
            }
            return -1;
        });

        _lastModified = new Lazy<DateTimeOffset>(() =>
        {
            try
            {
                if (_exists.Value)
                {
                    var properties = _blobClient.GetProperties().Value;
                    return properties.LastModified;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting blob last modified: {BlobName}", _blobClient.Name);
            }
            return DateTimeOffset.MinValue;
        });
    }

    public bool Exists => _exists.Value;
    public long Length => _length.Value;
    public string? PhysicalPath => null; // No physical path for blob storage
    public string Name => Path.GetFileName(_subpath);
    public DateTimeOffset LastModified => _lastModified.Value;
    public bool IsDirectory => false; // We're only handling files for now

    public Stream CreateReadStream()
    {
        if (!Exists)
        {
            throw new FileNotFoundException($"Blob not found: {_blobClient.Name}");
        }

        try
        {
            var download = _blobClient.DownloadStreaming();
            return download.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creating read stream for blob: {BlobName}", _blobClient.Name);
            throw;
        }
    }
}
