namespace QuillKit.Models;

/// <summary>
/// 📄 Represents the YAML front matter metadata for posts and pages
/// </summary>
public class PostMetadata
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Type { get; set; } = "Post";
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public List<string> Categories { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public string? Image { get; set; }
    public string? Link { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Excerpt { get; set; }

    /// <summary>
    /// 🔄 Converts metadata to Post model with validation
    /// </summary>
    /// <returns>Post object if valid, null if required fields are missing</returns>
    public Post? ToPost(string content, string fileName, out string? validationError, SiteConfig? siteConfig = null)
    {
        validationError = null;
        
        // ✅ Validate required fields
        if (string.IsNullOrWhiteSpace(Title))
        {
            validationError = "Missing required field: title";
            return null;
        }
        
        if (string.IsNullOrWhiteSpace(Slug))
        {
            validationError = "Missing required field: slug";
            return null;
        }
        
        // Validate date - if it's exactly DateTime.UtcNow default, it wasn't set
        if (Date == default(DateTime))
        {
            validationError = "Missing required field: date";
            return null;
        }
        
        // Handle timezone conversion for dates
        var pubDate = Date;
        if (siteConfig != null && Date.Kind == DateTimeKind.Unspecified)
        {
            // If the date has no timezone info, treat it as being in the site's timezone
            var timeZone = siteConfig.GetTimeZone();
            pubDate = TimeZoneInfo.ConvertTimeToUtc(Date, timeZone);
        }
        
        // Use site config author as default if not specified
        var author = !string.IsNullOrWhiteSpace(Author) 
            ? Author 
            : siteConfig?.Author?.Name ?? "Unknown";

        var post = new Post
        {
            Title = Title,
            Author = author,
            Type = Enum.TryParse<PostType>(Type, true, out var postType) ? postType : PostType.Post,
            PubDate = pubDate,
            Categories = Categories ?? new List<string>(),
            Tags = Tags ?? new List<string>(),
            Image = Image,
            Link = Link,
            Slug = Slug,
            Description = Description,
            Status = Enum.TryParse<PostStatus>(Status, true, out var postStatus) ? postStatus : PostStatus.Draft,
            Excerpt = Excerpt,
            Content = content,
            FileName = fileName,
            LastModified = File.Exists(fileName) ? File.GetLastWriteTime(fileName) : DateTime.UtcNow
        };

        return post;
    }
}
