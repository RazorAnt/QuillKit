namespace QuillKit.Models;

/// <summary>
/// 🔄 Defines the content storage provider for posts, media, and configuration
/// </summary>
public enum ContentProvider
{
    /// <summary>
    /// 📁 Local file system storage (development and self-hosted)
    /// </summary>
    Local,
    
    /// <summary>
    /// ☁️ Azure Blob Storage (cloud hosting)
    /// </summary>
    AzureBlob
}
