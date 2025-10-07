using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace QuillKit.Services;

/// <summary>
/// ☁️ Azure Blob Storage content service for production cloud hosting
/// </summary>
public class AzureBlobContentService : IContentService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureBlobContentService> _logger;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName = "content";

    public AzureBlobContentService(
        IConfiguration configuration,
        ILogger<AzureBlobContentService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var connectionString = _configuration.GetConnectionString("AzureStorage");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Azure Storage connection string is required for AzureBlobContentService");
        }

        _blobServiceClient = new BlobServiceClient(connectionString);
        _logger.LogInformation("☁️ Azure Blob Storage content service initialized with container: {ContainerName}", _containerName);
    }

    public async Task<string> ReadFileAsync(string relativePath)
    {
        // 📖 Read file content from Azure Blob Storage
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(relativePath.Replace('\\', '/'));

        try
        {
            var response = await blobClient.DownloadContentAsync();
            var content = response.Value.Content.ToString();
            _logger.LogDebug("📖 Successfully read blob: {RelativePath}", relativePath);
            return content;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("☁️ Blob not found: {RelativePath}", relativePath);
            throw new FileNotFoundException($"File not found: {relativePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to read blob: {RelativePath}", relativePath);
            throw;
        }
    }

    public async Task WriteFileAsync(string relativePath, string content)
    {
        // 💾 Write file content to Azure Blob Storage
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobClient = containerClient.GetBlobClient(relativePath.Replace('\\', '/'));

        try
        {
            await blobClient.UploadAsync(BinaryData.FromString(content), overwrite: true);
            _logger.LogDebug("💾 Successfully wrote blob: {RelativePath}", relativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to write blob: {RelativePath}", relativePath);
            throw;
        }
    }

    public async Task<bool> FileExistsAsync(string relativePath)
    {
        // 🔍 Check if blob exists in Azure Blob Storage
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(relativePath.Replace('\\', '/'));

        try
        {
            var response = await blobClient.ExistsAsync();
            var exists = response.Value;
            _logger.LogDebug("🔍 Blob exists check for {RelativePath}: {Exists}", relativePath, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to check blob existence: {RelativePath}", relativePath);
            throw;
        }
    }

    public async Task DeleteFileAsync(string relativePath)
    {
        // 🗑️ Delete blob from Azure Blob Storage
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(relativePath.Replace('\\', '/'));

        try
        {
            await blobClient.DeleteIfExistsAsync();
            _logger.LogDebug("🗑️ Successfully deleted blob: {RelativePath}", relativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to delete blob: {RelativePath}", relativePath);
            throw;
        }
    }

    public async Task<IEnumerable<string>> ListFilesAsync(string relativePath, string searchPattern = "*")
    {
        // 📋 List blobs in Azure Blob Storage
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

        try
        {
            var prefix = string.IsNullOrEmpty(relativePath) ? "" : relativePath.Replace('\\', '/');
            if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith("/"))
            {
                prefix += "/";
            }

            var blobs = new List<string>();
            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix))
            {
                // Simple pattern matching (supports * wildcard and *.ext patterns)
                if (searchPattern == "*" || searchPattern == "*.*")
                {
                    // Match all files
                    blobs.Add(blobItem.Name);
                }
                else if (searchPattern.StartsWith("*."))
                {
                    // Match specific extension (e.g., *.md)
                    var extension = searchPattern.Substring(1); // Remove the *
                    if (blobItem.Name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                    {
                        blobs.Add(blobItem.Name);
                    }
                }
                else if (searchPattern.EndsWith("*"))
                {
                    // Match prefix (e.g., test*)
                    var namePrefix = searchPattern.Substring(0, searchPattern.Length - 1);
                    if (blobItem.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        blobs.Add(blobItem.Name);
                    }
                }
                else
                {
                    // Exact match
                    if (blobItem.Name.Equals(searchPattern, StringComparison.OrdinalIgnoreCase))
                    {
                        blobs.Add(blobItem.Name);
                    }
                }
            }

            _logger.LogDebug("📋 Found {Count} blobs with prefix {Prefix}", blobs.Count, prefix);
            return blobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to list blobs with prefix: {RelativePath}", relativePath);
            throw;
        }
    }

    public async Task<DateTime> GetLastModifiedAsync(string relativePath)
    {
        // 🕐 Get blob last modified time from Azure Blob Storage
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(relativePath.Replace('\\', '/'));

        try
        {
            var properties = await blobClient.GetPropertiesAsync();
            var lastModified = properties.Value.LastModified.UtcDateTime;
            _logger.LogDebug("🕐 Last modified for {RelativePath}: {LastModified}", relativePath, lastModified);
            return lastModified;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("🕐 Cannot get last modified - blob not found: {RelativePath}", relativePath);
            throw new FileNotFoundException($"File not found: {relativePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to get blob last modified time: {RelativePath}", relativePath);
            throw;
        }
    }

    public async Task<IEnumerable<string>> ListDirectoriesAsync(string relativePath)
    {
        // 📂 List "directories" (blob prefixes) in Azure Blob Storage
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

        try
        {
            var prefix = string.IsNullOrEmpty(relativePath) ? "" : relativePath.Replace('\\', '/');
            if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith("/"))
            {
                prefix += "/";
            }

            var directories = new HashSet<string>();
            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix))
            {
                var blobPath = blobItem.Name;
                if (blobPath.StartsWith(prefix))
                {
                    var relativeToPrefixPath = blobPath.Substring(prefix.Length);
                    var firstSlashIndex = relativeToPrefixPath.IndexOf('/');
                    if (firstSlashIndex > 0)
                    {
                        var directoryName = relativeToPrefixPath.Substring(0, firstSlashIndex);
                        var fullDirectoryPath = string.IsNullOrEmpty(relativePath) 
                            ? directoryName 
                            : $"{relativePath.Replace('\\', '/')}/{directoryName}";
                        directories.Add(fullDirectoryPath);
                    }
                }
            }

            var directoryList = directories.ToList();
            _logger.LogDebug("📂 Found {Count} directories with prefix {Prefix}", directoryList.Count, prefix);
            return directoryList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to list directories with prefix: {RelativePath}", relativePath);
            throw;
        }
    }
}
