using QuillKit.Services;

namespace QuillKit.Extensions;

/// <summary>
/// 🧪 Quick Azure Blob Storage connection test
/// </summary>
public static class AzureTestHelper
{
    /// <summary>
    /// 🔍 Test Azure Blob Storage connection and list container contents
    /// </summary>
    public static async Task<bool> TestAzureConnection(IContentService contentService, ILogger logger)
    {
        try
        {
            logger.LogInformation("🧪 Testing Azure Blob Storage connection...");
            
            // Try to list files in the root
            var files = await contentService.ListFilesAsync("", "*.*");
            var fileList = files.ToList();
            
            logger.LogInformation("✅ Azure connection successful! Found {FileCount} files", fileList.Count);
            
            // Log first few files as examples
            foreach (var file in fileList.Take(5))
            {
                logger.LogInformation("📄 Found file: {FileName}", file);
            }
            
            if (fileList.Count > 5)
            {
                logger.LogInformation("... and {MoreCount} more files", fileList.Count - 5);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Azure connection test failed");
            return false;
        }
    }
}
