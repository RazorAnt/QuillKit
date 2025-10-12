using QuillKit.Models;

namespace QuillKit.Services;

/// <summary>
/// 📚 In-memory cached post service with content service integration
/// </summary>
public class FilePostService : IPostService
{
    private readonly string _contentPath;
    private readonly IContentService _contentService;
    private readonly PostParser _postParser;
    private readonly SiteConfigService _siteConfigService;
    private readonly ILogger<FilePostService> _logger;
    private readonly Dictionary<string, Post> _postCache;
    private readonly Dictionary<string, string> _parseErrors;
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
        _parseErrors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Load all posts into memory
        LoadAllPostsFromDiskAsync().GetAwaiter().GetResult();

        _logger.LogInformation(" Loaded {PostCount} posts into memory", _postCache.Count);
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
                    var result = await _postParser.ParseMarkdownFileAsync(_contentService, file, _siteConfigService.Config);
                    
                    lock (_cacheLock)
                    {
                        if (result.Success && result.Post != null)
                        {
                            _postCache[result.Post.Slug] = result.Post;
                            // Remove error if file was previously failing but now parses
                            _parseErrors.Remove(file);
                        }
                        else if (!string.IsNullOrEmpty(result.Error))
                        {
                            _parseErrors[file] = result.Error;
                            _logger.LogWarning("⚠️ Failed to parse {FileName}: {Error}", file, result.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (_cacheLock)
                    {
                        _parseErrors[file] = ex.Message;
                    }
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
    /// 🔄 Reload all posts from content service (manual refresh for both local and Azure storage)
    /// </summary>
    public async Task ReloadPostsAsync()
    {
        // 🔄 Reload all posts from content service
        _logger.LogInformation("� Manually reloading all posts from content service...");
        await LoadAllPostsFromDiskAsync();
        _logger.LogInformation("✅ Reload complete: {PostCount} posts in cache", _postCache.Count);
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
    public Task<List<Post>> GetPublishedPostsAsync(int page = 1, int pageSize = 5, bool includeDrafts = false)
    {
        lock (_cacheLock)
        {
            var query = _postCache.Values.Where(p => p.Type == PostType.Post);
            
            // 🔐 Include drafts only if explicitly requested (for admin preview)
            if (!includeDrafts)
            {
                query = query.Where(p => p.Status == PostStatus.Published);
            }
            
            var posts = query
                .OrderByDescending(p => p.PubDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            return Task.FromResult(posts);
        }
    }

    /// <summary>
    /// 📊 Gets the total count of published posts
    /// </summary>
    public Task<int> GetPublishedPostsCountAsync(bool includeDrafts = false)
    {
        lock (_cacheLock)
        {
            var query = _postCache.Values.Where(p => p.Type == PostType.Post);
            
            // 🔐 Include drafts only if explicitly requested (for admin preview)
            if (!includeDrafts)
            {
                query = query.Where(p => p.Status == PostStatus.Published);
            }
            
            var count = query.Count();
            
            return Task.FromResult(count);
        }
    }

    /// <summary>
    /// 🔍 Gets a post by its slug from memory cache
    /// </summary>
    public Task<Post?> GetPostBySlugAsync(string slug, bool includeDrafts = false)
    {
        lock (_cacheLock)
        {
            _postCache.TryGetValue(slug, out var post);
            
            // 🔐 Filter out drafts unless explicitly requested (for admin preview)
            if (post != null && !includeDrafts && post.Status == PostStatus.Draft)
            {
                return Task.FromResult<Post?>(null);
            }
            
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
    /// � Searches posts and pages by term (title, content, description, tags, categories)
    /// </summary>
    public Task<List<Post>> SearchPostsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Task.FromResult(new List<Post>());
        }

        lock (_cacheLock)
        {
            var normalizedSearch = searchTerm.ToLowerInvariant();
            
            var searchResults = _postCache.Values
                .Where(p => p.Status == PostStatus.Published)
                .Where(p => 
                    p.Title.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                    p.Content.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                    (p.Description != null && p.Description.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)) ||
                    (p.Excerpt != null && p.Excerpt.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)) ||
                    p.Tags.Any(t => t.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)) ||
                    p.Categories.Any(c => c.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                )
                .OrderByDescending(p => p.PubDate)
                .ToList();
            
            return Task.FromResult(searchResults);
        }
    }

    /// <summary>
    /// �💾 Saves a post to the file system and updates cache
    /// </summary>
    public async Task<Post> SavePostAsync(Post post)
    {
        _logger.LogInformation("💾 SavePostAsync started - Title: {Title}, FileName: {FileName}", post.Title, post.FileName);
        
        // Ensure slug
        if (string.IsNullOrEmpty(post.Slug))
        {
            post.Slug = GenerateSlug(post.Title);
        }

        // Determine target file name. If the post already has a FileName (editing existing file), prefer that.
        var targetFileName = !string.IsNullOrEmpty(post.FileName) ? post.FileName : $"{post.Slug}.md";
        _logger.LogInformation("📝 Target file name: {FileName}", targetFileName);

        // If the slug doesn't match the FileName, ensure FileName's slug is in sync for cache keying
        var resultingSlug = Path.GetFileNameWithoutExtension(targetFileName);
        post.Slug = resultingSlug;

        _logger.LogInformation("📝 Serializing to markdown...");
        var markdownContent = _postParser.SerializeToMarkdown(post);

        _logger.LogInformation("📝 Writing file to content service: {FileName}", targetFileName);
        await _contentService.WriteFileAsync(targetFileName, markdownContent);
        _logger.LogInformation("✅ File written successfully");

        _logger.LogInformation("📖 Re-parsing the saved file: {FileName}", targetFileName);
        // Re-parse the file to get the complete Post object with all metadata
        var parseResult = await _postParser.ParseMarkdownFileAsync(_contentService, targetFileName, _siteConfigService.Config);
        
        if (!parseResult.Success || parseResult.Post == null)
        {
            _logger.LogError("❌ Failed to re-parse saved post: {FileName}. Error: {Error}", targetFileName, parseResult.Error);
            throw new InvalidOperationException($"Failed to re-parse saved post: {targetFileName}. Error: {parseResult.Error}");
        }
        _logger.LogInformation("✅ File re-parsed successfully");

        var savedPost = parseResult.Post;

        _logger.LogInformation("🔄 Updating cache...");
        // Update cache immediately - remove any old entry for a previous slug if necessary
        lock (_cacheLock)
        {
            // Remove entries where FileName matched a different slug previously
            var keysToRemove = _postCache.Keys.Where(k => string.Equals(_postCache[k].FileName, targetFileName, StringComparison.OrdinalIgnoreCase) && k != savedPost.Slug).ToList();
            foreach (var k in keysToRemove)
            {
                _postCache.Remove(k);
            }

            _postCache[savedPost.Slug] = savedPost;
            
            // Remove any parse error for this file since it saved successfully
            _parseErrors.Remove(targetFileName);
        }

        _logger.LogInformation("💾 Saved post: {Title} to {FileName}, updated cache with slug: {Slug}", savedPost.Title, targetFileName, savedPost.Slug);

        return savedPost;
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
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".mp4", ".webm", ".pdf", ".zip" };
            
            var mediaFiles = await _contentService.ListFilesAsync("media", "*.*");
            _logger.LogInformation("🖼️ Found {Count} files from ListFilesAsync('media', '*.*')", mediaFiles.Count());
            
            foreach (var file in mediaFiles)
            {
                _logger.LogInformation("  - File: {File}", file);
            }
            
            // Keep the relative path but remove the "media/" prefix (e.g., "media/2011/07/picture.jpg" -> "2011/07/picture.jpg")
            var result = mediaFiles
                .Where(f => !string.IsNullOrEmpty(f) && allowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Select(f => f.StartsWith("media/") ? f.Substring(6) : f) // Remove "media/" prefix
                .ToList();
            
            _logger.LogInformation("🖼️ Returning {Count} media files after filtering", result.Count);
            return result;
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
    /// 🚨 Gets the list of parse errors for files that couldn't be loaded
    /// </summary>
    public Task<Dictionary<string, string>> GetParseErrorsAsync()
    {
        lock (_cacheLock)
        {
            return Task.FromResult(new Dictionary<string, string>(_parseErrors));
        }
    }

    /// <summary>
    /// 📄 Gets raw file content without parsing or validation
    /// </summary>
    public async Task<string?> GetRawFileContentAsync(string fileName)
    {
        try
        {
            if (!await _contentService.FileExistsAsync(fileName))
            {
                return null;
            }

            return await _contentService.ReadFileAsync(fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error reading raw file: {FileName}", fileName);
            return null;
        }
    }

    /// <summary>
    /// 💾 Saves raw file content and reloads the post cache
    /// </summary>
    public async Task SaveRawFileAsync(string fileName, string content)
    {
        try
        {
            _logger.LogInformation("📝 Writing raw file: {FileName}", fileName);
            await _contentService.WriteFileAsync(fileName, content);
            _logger.LogInformation("✅ Raw file saved successfully");

            // Reload all posts to update cache and clear any parse errors
            await LoadAllPostsFromDiskAsync();
            _logger.LogInformation("🔄 Posts reloaded after raw file save");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error saving raw file: {FileName}", fileName);
            throw;
        }
    }
}
