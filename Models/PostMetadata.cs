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
    /// 🔄 Converts metadata to Post model
    /// </summary>
    public Post ToPost(string content, string fileName)
    {
        var post = new Post
        {
            Title = Title,
            Author = Author,
            Type = Enum.TryParse<PostType>(Type, true, out var postType) ? postType : PostType.Post,
            PubDate = Date,
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
