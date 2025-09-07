namespace QuillKit.Services;

/// <summary>
/// 📁 Local file system content service for development and file-based hosting
/// </summary>
public class LocalFileContentService : IContentService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LocalFileContentService> _logger;
    private readonly string _localContentPath;

    public LocalFileContentService(
        IWebHostEnvironment environment,
        ILogger<LocalFileContentService> logger)
    {
        _environment = environment;
        _logger = logger;
        _localContentPath = Path.Combine(_environment.ContentRootPath, "Content");

        // Ensure the content directory exists
        if (!Directory.Exists(_localContentPath))
        {
            Directory.CreateDirectory(_localContentPath);
            _logger.LogInformation("📁 Created content directory at {ContentPath}", _localContentPath);
        }
    }

    public async Task<string> ReadFileAsync(string relativePath)
    {
        // 📖 Read file content from local file system
        var fullPath = Path.Combine(_localContentPath, relativePath);
        
        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("📁 File not found: {RelativePath} (Full path: {FullPath})", relativePath, fullPath);
            throw new FileNotFoundException($"File not found: {relativePath}");
        }

        try
        {
            var content = await File.ReadAllTextAsync(fullPath);
            _logger.LogDebug("📖 Successfully read file: {RelativePath}", relativePath);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to read file: {RelativePath}", relativePath);
            throw;
        }
    }

    public async Task WriteFileAsync(string relativePath, string content)
    {
        // 💾 Write file content to local file system
        var fullPath = Path.Combine(_localContentPath, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.LogDebug("📁 Created directory: {Directory}", directory);
        }

        try
        {
            await File.WriteAllTextAsync(fullPath, content);
            _logger.LogDebug("💾 Successfully wrote file: {RelativePath}", relativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to write file: {RelativePath}", relativePath);
            throw;
        }
    }

    public async Task<bool> FileExistsAsync(string relativePath)
    {
        // 🔍 Check if file exists in local file system
        var fullPath = Path.Combine(_localContentPath, relativePath);
        var exists = File.Exists(fullPath);
        
        _logger.LogDebug("🔍 File exists check for {RelativePath}: {Exists}", relativePath, exists);
        return await Task.FromResult(exists);
    }

    public async Task DeleteFileAsync(string relativePath)
    {
        // 🗑️ Delete file from local file system
        var fullPath = Path.Combine(_localContentPath, relativePath);
        
        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("🗑️ Cannot delete file - not found: {RelativePath}", relativePath);
            return;
        }

        try
        {
            File.Delete(fullPath);
            _logger.LogDebug("🗑️ Successfully deleted file: {RelativePath}", relativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to delete file: {RelativePath}", relativePath);
            throw;
        }

        await Task.CompletedTask;
    }

    public async Task<IEnumerable<string>> ListFilesAsync(string relativePath, string searchPattern = "*")
    {
        // 📋 List files in local directory
        var searchPath = string.IsNullOrEmpty(relativePath) 
            ? _localContentPath 
            : Path.Combine(_localContentPath, relativePath);

        if (!Directory.Exists(searchPath))
        {
            _logger.LogWarning("📋 Directory not found for listing: {SearchPath}", searchPath);
            return Enumerable.Empty<string>();
        }

        try
        {
            var files = Directory.GetFiles(searchPath, searchPattern, SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(_localContentPath, f))
                .Select(f => f.Replace('\\', '/')); // Normalize path separators

            var fileList = files.ToList();
            _logger.LogDebug("📋 Found {Count} files in {SearchPath}", fileList.Count, searchPath);
            
            return await Task.FromResult(fileList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to list files in: {SearchPath}", searchPath);
            throw;
        }
    }

    public async Task<DateTime> GetLastModifiedAsync(string relativePath)
    {
        // 🕐 Get file last modified time from local file system
        var fullPath = Path.Combine(_localContentPath, relativePath);
        
        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("🕐 Cannot get last modified - file not found: {RelativePath}", relativePath);
            throw new FileNotFoundException($"File not found: {relativePath}");
        }

        try
        {
            var lastModified = File.GetLastWriteTimeUtc(fullPath);
            _logger.LogDebug("🕐 Last modified for {RelativePath}: {LastModified}", relativePath, lastModified);
            return await Task.FromResult(lastModified);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to get last modified time: {RelativePath}", relativePath);
            throw;
        }
    }

    public async Task<IEnumerable<string>> ListDirectoriesAsync(string relativePath)
    {
        // 📂 List directories in local file system
        var searchPath = string.IsNullOrEmpty(relativePath) 
            ? _localContentPath 
            : Path.Combine(_localContentPath, relativePath);

        if (!Directory.Exists(searchPath))
        {
            _logger.LogWarning("📂 Directory not found for listing: {SearchPath}", searchPath);
            return Enumerable.Empty<string>();
        }

        try
        {
            var directories = Directory.GetDirectories(searchPath, "*", SearchOption.TopDirectoryOnly)
                .Select(d => Path.GetRelativePath(_localContentPath, d))
                .Select(d => d.Replace('\\', '/')); // Normalize path separators

            var directoryList = directories.ToList();
            _logger.LogDebug("📂 Found {Count} directories in {SearchPath}", directoryList.Count, searchPath);
            
            return await Task.FromResult(directoryList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to list directories in: {SearchPath}", searchPath);
            throw;
        }
    }
}
