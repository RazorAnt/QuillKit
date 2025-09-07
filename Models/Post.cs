using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Markdig;

namespace QuillKit.Models;

public class Post
{
    public Post()
    {
        Type = PostType.Post;
        Status = PostStatus.Draft;
        Tags = new List<string>();
        Categories = new List<string>();
        PubDate = DateTime.UtcNow;
        LastModified = DateTime.UtcNow;
    }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    public PostType Type { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Author { get; set; } = string.Empty;
    
    public DateTime PubDate { get; set; }
    
    public List<string> Categories { get; set; }
    
    public List<string> Tags { get; set; }
    
    [Url]
    public string? Image { get; set; }
    
    [Url]
    public string? Link { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Slug { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Excerpt { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public PostStatus Status { get; set; }
    
    [Required]
    public string Content { get; set; } = string.Empty; // In Markdown
    
    public string FileName { get; set; } = string.Empty; // Retrieved from File
    
    public DateTime LastModified { get; set; } // Retrieved from File

    public string GetAbsoluteUrl(HttpRequest request)
    {
        var scheme = request.Scheme;
        var host = request.Host.ToString();
        return $"{scheme}://{host}/post/{Slug}";
    }

    public string GetUrl()
    {
        return $"/post/{Slug}";
    }

    public string AutoExcerpt()
    {
        if (!string.IsNullOrWhiteSpace(Excerpt))
            return Excerpt;
        
        if (string.IsNullOrWhiteSpace(Content))
            return string.Empty;

        var htmlContent = GetHtmlContent();
        var regexStripHtml = new Regex("<[^>]*>", RegexOptions.Compiled);
        var clean = regexStripHtml.Replace(htmlContent, string.Empty).Trim();
        
        clean = clean.Replace("&#160;", "")
                    .Replace("&nbsp;", "")
                    .Replace("&rsquo;", "'");

        if (clean.Length > 150)
            clean = clean.Substring(0, 150) + "...";

        return clean;
    }

    public string GetHtmlContent()
    {
        var postContent = Content;

        // Custom shortcode processing
        postContent = ProcessShortcodes(postContent);

        // Convert markdown to HTML using Markdig (modern alternative to CommonMark)
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
            
        return Markdown.ToHtml(postContent, pipeline);
    }

    private string ProcessShortcodes(string content)
    {
        // Generate a unique gallery ID for this post
        var galleryId = Guid.NewGuid().ToString("N")[..8];
        
        // Vimeo helper [vimeo:123456789]
        var vimeo = "<div class=\"video\"><iframe src=\"https://player.vimeo.com/video/{0}\" frameborder=\"0\" webkitallowfullscreen allowfullscreen></iframe></div>";
        content = Regex.Replace(content, @"\[vimeo:(.*?)\]", match => string.Format(vimeo, match.Groups[1].Value));

        // YouTube helper [youtube:xyzAbc123]
        var youtube = "<div class=\"video\"><iframe src=\"//www.youtube.com/embed/{0}?modestbranding=1&amp;theme=light\" allowfullscreen></iframe></div>";
        content = Regex.Replace(content, @"\[youtube:(.*?)\]", match => string.Format(youtube, match.Groups[1].Value));

        // Lightbox helper [lightbox src=""]
        var lightbox = "<a href=\"{0}\" class=\"lightbox\" rel=\"lightbox-single\"><img src=\"{0}\" alt=\"\" /></a>";
        content = Regex.Replace(content, @"\[lightbox src=""(.*?)\""]", match => string.Format(lightbox, match.Groups[1].Value));

        // LightboxLeft helper [lightboxleft src=""]
        var lightboxleft = "<a href=\"{0}\" class=\"lightbox\" style=\"float: left;\" rel=\"lightbox-single\"><img src=\"{0}\" alt=\"\" /></a>";
        content = Regex.Replace(content, @"\[lightboxleft src=""(.*?)\""]", match => string.Format(lightboxleft, match.Groups[1].Value));

        // LightboxRight helper [lightboxright src=""]
        var lightboxright = "<a href=\"{0}\" class=\"lightbox\" style=\"float: right;\" rel=\"lightbox-single\"><img src=\"{0}\" alt=\"\" /></a>";
        content = Regex.Replace(content, @"\[lightboxright src=""(.*?)\""]", match => string.Format(lightboxright, match.Groups[1].Value));
        content = Regex.Replace(content, @"\[lightboxright src=([^\]]+)\]", match => string.Format(lightboxright, match.Groups[1].Value.Trim('"')));

        // LightboxMax helper [lightboxmax src=""]
        var lightboxmax = "<a href=\"{0}\" class=\"lightbox lightbox-max\" rel=\"lightbox-single\"><img src=\"{0}\" alt=\"\" style=\"width: 100%; max-width: 100%;\" /></a>";
        content = Regex.Replace(content, @"\[lightboxmax src=""(.*?)\""]", match => string.Format(lightboxmax, match.Groups[1].Value));

        // Gallery column helpers - handle both quoted and unquoted src values, use gallery-specific rel
        var lightbox1 = $"<div class=\"gallery-col-1\"><a href=\"{{0}}\" class=\"lightbox\" rel=\"gallery-{galleryId}\"><img src=\"{{0}}\" alt=\"\" /></a></div>";
        content = Regex.Replace(content, @"\[lightbox1 src=""(.*?)\""]", match => string.Format(lightbox1, match.Groups[1].Value));
        content = Regex.Replace(content, @"\[lightbox1 src=([^\]]+)\]", match => string.Format(lightbox1, match.Groups[1].Value.Trim('"')));

        var lightbox2 = $"<div class=\"gallery-col-2\"><a href=\"{{0}}\" class=\"lightbox\" rel=\"gallery-{galleryId}\"><img src=\"{{0}}\" alt=\"\" /></a></div>";
        content = Regex.Replace(content, @"\[lightbox2 src=""(.*?)\""]", match => string.Format(lightbox2, match.Groups[1].Value));
        content = Regex.Replace(content, @"\[lightbox2 src=([^\]]+)\]", match => string.Format(lightbox2, match.Groups[1].Value.Trim('"')));

        var lightbox3 = $"<div class=\"gallery-col-3\"><a href=\"{{0}}\" class=\"lightbox\" rel=\"gallery-{galleryId}\"><img src=\"{{0}}\" alt=\"\" /></a></div>";
        content = Regex.Replace(content, @"\[lightbox3 src=""(.*?)\""]", match => string.Format(lightbox3, match.Groups[1].Value));
        content = Regex.Replace(content, @"\[lightbox3 src=([^\]]+)\]", match => string.Format(lightbox3, match.Groups[1].Value.Trim('"')));

        // Gallery wrapper [gallery]...[/gallery]
        var galleryWrapper = "<div class=\"gallery\">$1</div>";
        content = Regex.Replace(content, @"\[gallery\](.*?)\[/gallery\]", galleryWrapper, RegexOptions.Singleline);

        // LeftRightClear helper [leftrightclear]
        var lrclear = "<div style=\"clear: both;\"></div>";
        content = Regex.Replace(content, @"\[leftrightclear]", _ => lrclear);

        return content;
    }
}
