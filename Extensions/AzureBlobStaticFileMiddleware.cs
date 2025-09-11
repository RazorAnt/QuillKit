using Azure.Storage.Blobs;

namespace QuillKit.Extensions;

/// <summary>
/// 🌐 Middleware to serve static files from Azure Blob Storage
/// Handles /media/* and /assets/* requests by proxying to blob storage
/// </summary>
public class AzureBlobStaticFileMiddleware
{
    private readonly RequestDelegate _next;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureBlobStaticFileMiddleware> _logger;
    private readonly string _containerName = "content";

    public AzureBlobStaticFileMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<AzureBlobStaticFileMiddleware> logger)
    {
        _next = next;
        _logger = logger;

        var connectionString = configuration.GetConnectionString("AzureStorage");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Azure Storage connection string is required for blob static files");
        }

        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;

        // Check if this is a request for media or assets
        if (path != null && (path.StartsWith("/media/") || path.StartsWith("/assets/")))
        {
            try
            {
                // Convert URL path to blob path
                var blobPath = ConvertUrlToBlobPath(path);
                
                // Get the blob
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(blobPath);

                if (await blobClient.ExistsAsync())
                {
                    // Get blob properties for content type
                    var properties = await blobClient.GetPropertiesAsync();
                    
                    // Set appropriate headers
                    context.Response.ContentType = properties.Value.ContentType ?? GetContentType(path);
                    context.Response.Headers.CacheControl = "public, max-age=31536000"; // Cache for 1 year
                    
                    // Stream the blob content
                    var download = await blobClient.DownloadStreamingAsync();
                    await download.Value.Content.CopyToAsync(context.Response.Body);
                    
                    _logger.LogDebug("📁 Served blob: {BlobPath}", blobPath);
                    return;
                }
                else
                {
                    _logger.LogWarning("📁 Blob not found: {BlobPath}", blobPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error serving blob: {Path}", path);
            }
        }

        // Continue to next middleware if not handled
        await _next(context);
    }

    private string ConvertUrlToBlobPath(string urlPath)
    {
        // Remove leading slash and convert URL path to blob path
        // /media/image.jpg -> media/image.jpg
        // /assets/css/style.css -> Theme/assets/css/style.css
        
        if (urlPath.StartsWith("/media/"))
        {
            return urlPath.Substring(1); // Remove leading /
        }
        
        if (urlPath.StartsWith("/assets/"))
        {
            return "Theme/" + urlPath.Substring(1); // /assets/... -> Theme/assets/...
        }
        
        return urlPath.Substring(1); // Fallback: remove leading /
    }

    private string GetContentType(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".pdf" => "application/pdf",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            _ => "application/octet-stream"
        };
    }
}
