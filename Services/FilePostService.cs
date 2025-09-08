using QuillKit.Models;

namespace QuillKit.Services;

/// <summary>
/// 📚 In-memory cached post service with content service integration
/// </summary>
public class FilePostService : IPostService, IDisposable
{
    private readonly string _contentPath;
    private readonly IContentService _contentService;
    private readonly PostParser _postParser;
    private readonly SiteConfigService _siteConfigService;
    private readonly ILogger<FilePostService> _logger;
    private readonly Dictionary<string, Post> _postCache;
    private readonly FileSystemWatcher? _fileWatcher;
    private readonly object _cacheLock = new();

    public FilePostService(
        IConfiguration configuration, 
        IContentService contentService,
        PostParser postParser, 
        SiteConfigService siteConfigService,
        ILogger<FilePostService> logger)
    {
        _contentPath = configuration["ContentPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Content");
        _contentService = contentService;
        _postParser = postParser;
        _siteConfigService = siteConfigService;
        _logger = logger;
        _postCache = new Dictionary<string, Post>(StringComparer.OrdinalIgnoreCase);

        // Load all posts into memory
        LoadAllPostsFromDiskAsync().GetAwaiter().GetResult();

        // Set up file system watcher only for local storage
        var contentProvider = configuration.GetValue<string>("ContentProvider", "Local");
        if (contentProvider.ToLowerInvariant() is "local" or "file")
        {
            // Ensure content directory exists for local storage
            if (!Directory.Exists(_contentPath))
            {
                Directory.CreateDirectory(_contentPath);
                _logger.LogInformation("📁 Created content directory: {ContentPath}", _contentPath);
            }

            _fileWatcher = new FileSystemWatcher(_contentPath, "*.md")
            {
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName
            };
            _fileWatcher.Created += OnFileChanged;
            _fileWatcher.Changed += OnFileChanged;
            _fileWatcher.Deleted += OnFileDeleted;
            _fileWatcher.Renamed += OnFileRenamed;
            _fileWatcher.EnableRaisingEvents = true;
        }

        _logger.LogInformation("📖 Loaded {PostCount} posts into memory with file watching {Status}", 
            _postCache.Count, 
            _fileWatcher != null ? "enabled" : "disabled (blob storage)");
    }

    /// <summary>
    /// 📚 Loads all markdown files from content service into memory cache
    /// </summary>
    private async Task LoadAllPostsFromDiskAsync()
    {
        lock (_cacheLock)
        {
            _postCache.Clear();
        }

        try
        {
            var markdownFiles = await _contentService.ListFilesAsync("", "*.md");

            foreach (var file in markdownFiles)
            {
                try
                {
                    var post = await _postParser.ParseMarkdownFileAsync(_contentService, file, _siteConfigService.Config);
                    if (post != null)
                    {
                        lock (_cacheLock)
                        {
                            _postCache[post.Slug] = post;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error loading file into cache: {FileName}", file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error loading posts from content service");
        }
    }

    /// <summary>
    /// 🔄 File change event handler
    /// </summary>
    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation("📝 File changed: {FileName}", e.Name);
        var relativePath = Path.GetFileName(e.Name ?? "");
        ReloadSinglePostAsync(relativePath).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 🗑️ File deleted event handler
    /// </summary>
    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation("🗑️ File deleted: {FileName}", e.Name);
        var slug = Path.GetFileNameWithoutExtension(e.Name ?? "");
        
        lock (_cacheLock)
        {
            _postCache.Remove(slug);
        }
    }

    /// <summary>
    /// 🔄 File renamed event handler
    /// </summary>
    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        _logger.LogInformation("📝 File renamed: {OldName} → {NewName}", e.OldName, e.Name);
        
        // Remove old entry
        var oldSlug = Path.GetFileNameWithoutExtension(e.OldName ?? "");
        lock (_cacheLock)
        {
            _postCache.Remove(oldSlug);
        }
        
        // Add new entry
        var relativePath = Path.GetFileName(e.Name ?? "");
        ReloadSinglePostAsync(relativePath).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 🔄 Reloads a single post from content service
    /// </summary>
    private async Task ReloadSinglePostAsync(string relativePath)
    {
        try
        {
            var post = await _postParser.ParseMarkdownFileAsync(_contentService, relativePath, _siteConfigService.Config);
            if (post != null)
            {
                lock (_cacheLock)
                {
                    _postCache[post.Slug] = post;
                }
                _logger.LogInformation("✅ Reloaded post: {Slug}", post.Slug);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error reloading file: {RelativePath}", relativePath);
        }
    }

    /// <summary>
    /// 📖 Gets all posts and pages from memory cache
    /// </summary>
    public Task<List<Post>> GetAllPostsAsync()
    {
        lock (_cacheLock)
        {
            return Task.FromResult(_postCache.Values.OrderByDescending(p => p.PubDate).ToList());
        }
    }

    /// <summary>
    /// 📰 Gets published posts with pagination from memory cache
    /// </summary>
    public Task<List<Post>> GetPublishedPostsAsync(int page = 1, int pageSize = 5)
    {
        lock (_cacheLock)
        {
            var publishedPosts = _postCache.Values
                .Where(p => p.Status == PostStatus.Published && p.Type == PostType.Post)
                .OrderByDescending(p => p.PubDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            return Task.FromResult(publishedPosts);
        }
    }

    /// <summary>
    /// 📊 Gets the total count of published posts
    /// </summary>
    public Task<int> GetPublishedPostsCountAsync()
    {
        lock (_cacheLock)
        {
            var count = _postCache.Values
                .Count(p => p.Status == PostStatus.Published && p.Type == PostType.Post);
            
            return Task.FromResult(count);
        }
    }

    /// <summary>
    /// 🔍 Gets a post by its slug from memory cache
    /// </summary>
    public Task<Post?> GetPostBySlugAsync(string slug)
    {
        lock (_cacheLock)
        {
            _postCache.TryGetValue(slug, out var post);
            return Task.FromResult(post);
        }
    }

    /// <summary>
    /// 📁 Gets a post by its file name
    /// </summary>
    public Task<Post?> GetPostByFileNameAsync(string fileName)
    {
        var slug = Path.GetFileNameWithoutExtension(fileName);
        return GetPostBySlugAsync(slug);
    }

    /// <summary>
    /// 🏷️ Gets posts by category from memory cache
    /// </summary>
    public Task<List<Post>> GetPostsByCategoryAsync(string category)
    {
        lock (_cacheLock)
        {
            var categoryPosts = _postCache.Values
                .Where(p => p.Categories.Any(c => c.Equals(category, StringComparison.OrdinalIgnoreCase)))
                .Where(p => p.Status == PostStatus.Published)
                .OrderByDescending(p => p.PubDate)
                .ToList();
            
            return Task.FromResult(categoryPosts);
        }
    }

    /// <summary>
    /// 🏷️ Gets posts by tag from memory cache
    /// </summary>
    public Task<List<Post>> GetPostsByTagAsync(string tag)
    {
        lock (_cacheLock)
        {
            var tagPosts = _postCache.Values
                .Where(p => p.Tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)))
                .Where(p => p.Status == PostStatus.Published)
                .OrderByDescending(p => p.PubDate)
                .ToList();
            
            return Task.FromResult(tagPosts);
        }
    }

    /// <summary>
    /// ✍️ Gets posts by author from memory cache
    /// </summary>
    public Task<List<Post>> GetPostsByAuthorAsync(string author)
    {
        lock (_cacheLock)
        {
            var authorPosts = _postCache.Values
                .Where(p => p.Author.Equals(author, StringComparison.OrdinalIgnoreCase))
                .Where(p => p.Status == PostStatus.Published)
                .OrderByDescending(p => p.PubDate)
                .ToList();
            
            return Task.FromResult(authorPosts);
        }
    }

    /// <summary>
    /// 💾 Saves a post to the file system and updates cache
    /// </summary>
    public async Task<Post> SavePostAsync(Post post)
    {
        if (string.IsNullOrEmpty(post.Slug))
        {
            post.Slug = GenerateSlug(post.Title);
        }

        var fileName = $"{post.Slug}.md";

        var markdownContent = _postParser.SerializeToMarkdown(post);
        
        await _contentService.WriteFileAsync(fileName, markdownContent);
        
        post.FileName = fileName;
        post.LastModified = DateTime.UtcNow;

        // Update cache immediately
        lock (_cacheLock)
        {
            _postCache[post.Slug] = post;
        }

        _logger.LogInformation("💾 Saved post: {Title} to {FileName}", post.Title, fileName);
        
        return post;
    }

    /// <summary>
    /// 🗑️ Deletes a post by slug
    /// </summary>
    public async Task DeletePostAsync(string slug)
    {
        var post = await GetPostBySlugAsync(slug);
        if (post != null)
        {
            var fileName = $"{slug}.md";
            await _contentService.DeleteFileAsync(fileName);
            
            // Remove from cache immediately
            lock (_cacheLock)
            {
                _postCache.Remove(slug);
            }
            
            _logger.LogInformation("🗑️ Deleted post: {Slug}", slug);
        }
    }

    /// <summary>
    /// 📂 Gets all unique categories from memory cache
    /// </summary>
    public Task<List<string>> GetAllCategoriesAsync()
    {
        lock (_cacheLock)
        {
            var categories = _postCache.Values
                .Where(p => p.Status == PostStatus.Published)
                .SelectMany(p => p.Categories)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();
            
            return Task.FromResult(categories);
        }
    }

    /// <summary>
    /// 🏷️ Gets all unique tags from memory cache
    /// </summary>
    public Task<List<string>> GetAllTagsAsync()
    {
        lock (_cacheLock)
        {
            var tags = _postCache.Values
                .Where(p => p.Status == PostStatus.Published)
                .SelectMany(p => p.Tags)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t)
                .ToList();
            
            return Task.FromResult(tags);
        }
    }

    /// <summary>
    /// ✍️ Gets all unique authors from memory cache
    /// </summary>
    public Task<List<string>> GetAllAuthorsAsync()
    {
        lock (_cacheLock)
        {
            var authors = _postCache.Values
                .Where(p => p.Status == PostStatus.Published)
                .Select(p => p.Author)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(a => a)
                .ToList();
            
            return Task.FromResult(authors);
        }
    }

    /// <summary>
    /// 🖼️ Gets all media files from the content directory
    /// </summary>
    public async Task<List<string>> GetMediaFilesAsync()
    {
        try
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".mp4", ".webm" };
            
            var mediaFiles = await _contentService.ListFilesAsync("media", "*.*");
            
            return mediaFiles
                .Where(f => allowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error loading media files");
            return new List<string>();
        }
    }

    /// <summary>
    /// 🔗 Generates a URL-friendly slug from a title
    /// </summary>
    private string GenerateSlug(string title)
    {
        return title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace("?", "")
            .Replace("!", "")
            .Replace(".", "")
            .Replace(",", "")
            .Replace(";", "")
            .Replace(":", "")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace("&", "and");
    }

    /// <summary>
    /// 🧹 Dispose resources and stop file watching
    /// </summary>
    public void Dispose()
    {
        _fileWatcher?.Dispose();
        _logger.LogInformation("📖 FilePostService disposed - file watching stopped");
    }
}
