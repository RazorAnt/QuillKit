using QuillKit.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Text.RegularExpressions;

namespace QuillKit.Services;

/// <summary>
/// 📊 Result of parsing a post with potential error information
/// </summary>
public class PostParseResult
{
    public Post? Post { get; set; }
    public string? Error { get; set; }
    public bool Success => Post != null && string.IsNullOrEmpty(Error);
}

/// <summary>
/// 📖 Parses markdown files with YAML front matter into Post objects
/// </summary>
public class PostParser
{
    private static readonly Regex FrontMatterRegex = new(@"^---\s*\n(.*?)\n---\s*\n(.*)$", 
        RegexOptions.Singleline | RegexOptions.Compiled);

    private readonly IDeserializer _yamlDeserializer;

    public PostParser()
    {
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// 🔍 Parses a markdown file with YAML front matter
    /// </summary>
    public Post? ParseMarkdownFile(string filePath, out string? parseError, SiteConfig? siteConfig = null)
    {
        parseError = null;
        
        if (!File.Exists(filePath))
        {
            parseError = "File not found";
            return null;
        }

        var content = File.ReadAllText(filePath);
        return ParseMarkdownContent(content, filePath, out parseError, siteConfig);
    }

    /// <summary>
    /// 🔍 Parses a markdown file with YAML front matter using content service
    /// </summary>
    public async Task<PostParseResult> ParseMarkdownFileAsync(IContentService contentService, string relativePath, SiteConfig? siteConfig = null)
    {
        // Check if file exists in the content service
        if (!await contentService.FileExistsAsync(relativePath))
        {
            return new PostParseResult { Error = $"File not found: {relativePath}" };
        }

        var markdown = await contentService.ReadFileAsync(relativePath);
        var post = ParseMarkdownContent(markdown, relativePath, out var parseError, siteConfig);
        
        return new PostParseResult 
        { 
            Post = post, 
            Error = parseError 
        };
    }

    /// <summary>
    /// 📝 Parses markdown content with YAML front matter
    /// </summary>
    public Post? ParseMarkdownContent(string content, string fileName, out string? parseError, SiteConfig? siteConfig = null)
    {
        parseError = null;
        var match = FrontMatterRegex.Match(content);
        
        if (!match.Success)
        {
            // ❌ No front matter found - this is now an error
            parseError = "No YAML front matter found";
            return null;
        }

        var yamlContent = match.Groups[1].Value;
        var markdownContent = match.Groups[2].Value.Trim();

        try
        {
            var metadata = _yamlDeserializer.Deserialize<PostMetadata>(yamlContent);
            var post = metadata.ToPost(markdownContent, fileName, out string? validationError, siteConfig);
            
            if (post == null)
            {
                // Validation failed
                parseError = validationError ?? "Unknown validation error";
                return null;
            }
            
            return post;
        }
        catch (Exception ex)
        {
            // ❌ YAML parsing failed
            parseError = $"YAML parse error: {ex.Message}";
            return null;
        }
    }

    /// <summary>
    /// 🛠️ Creates a basic post when YAML parsing fails
    /// </summary>
    private Post CreateBasicPost(string content, string fileName)
    {
        var title = Path.GetFileNameWithoutExtension(fileName)
            .Replace("-", " ")
            .Replace("_", " ");

        return new Post
        {
            Title = title,
            Author = "Unknown",
            Content = content,
            FileName = fileName,
            Slug = Path.GetFileNameWithoutExtension(fileName),
            Status = PostStatus.Draft,
            LastModified = File.Exists(fileName) ? File.GetLastWriteTime(fileName) : DateTime.UtcNow
        };
    }

    /// <summary>
    /// 💾 Serializes a Post back to markdown with YAML front matter
    /// </summary>
    public string SerializeToMarkdown(Post post)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var metadata = new PostMetadata
        {
            Title = post.Title,
            Author = post.Author,
            Type = post.Type.ToString(),
            Date = post.PubDate,
            Categories = post.Categories,
            Tags = post.Tags,
            Image = post.Image,
            Link = post.Link,
            Slug = post.Slug,
            Description = post.Description,
            Status = post.Status.ToString(),
            Excerpt = post.Excerpt
        };

        var yaml = serializer.Serialize(metadata);
        return $"---\n{yaml}---\n\n{post.Content}";
    }
}
