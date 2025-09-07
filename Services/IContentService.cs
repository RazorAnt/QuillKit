namespace QuillKit.Services;

/// <summary>
/// 📁 Service for reading and writing content files from various storage providers
/// </summary>
public interface IContentService
{
    Task<string> ReadFileAsync(string relativePath);
    Task WriteFileAsync(string relativePath, string content);
    Task<bool> FileExistsAsync(string relativePath);
    Task DeleteFileAsync(string relativePath);
    Task<IEnumerable<string>> ListFilesAsync(string relativePath, string searchPattern = "*");
    Task<IEnumerable<string>> ListDirectoriesAsync(string relativePath);
}
