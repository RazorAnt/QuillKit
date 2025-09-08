using QuillKit.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Text.RegularExpressions;

namespace QuillKit.Services;

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
    public Post? ParseMarkdownFile(string filePath, SiteConfig? siteConfig = null)
    {
        if (!File.Exists(filePath))
            return null;

        var content = File.ReadAllText(filePath);
        return ParseMarkdownContent(content, filePath, siteConfig);
    }

    /// <summary>
    /// 🔍 Parses a markdown file with YAML front matter using content service
    /// </summary>
    public async Task<Post?> ParseMarkdownFileAsync(IContentService contentService, string relativePath, SiteConfig? siteConfig = null)
    {
        if (!await contentService.FileExistsAsync(relativePath))
            return null;

        var content = await contentService.ReadFileAsync(relativePath);
        return ParseMarkdownContent(content, relativePath, siteConfig);
    }

    /// <summary>
    /// 📝 Parses markdown content with YAML front matter
    /// </summary>
    public Post? ParseMarkdownContent(string content, string fileName, SiteConfig? siteConfig = null)
    {
        var match = FrontMatterRegex.Match(content);
        
        if (!match.Success)
        {
            // No front matter found - create a basic post
            return CreateBasicPost(content, fileName);
        }

        var yamlContent = match.Groups[1].Value;
        var markdownContent = match.Groups[2].Value.Trim();

        try
        {
            var metadata = _yamlDeserializer.Deserialize<PostMetadata>(yamlContent);
            return metadata.ToPost(markdownContent, fileName, siteConfig);
        }
        catch (Exception ex)
        {
            // Log the error and return a basic post
            Console.WriteLine($"Error parsing YAML front matter in {fileName}: {ex.Message}");
            return CreateBasicPost(content, fileName);
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
