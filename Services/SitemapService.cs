using System.Text;
using System.Xml;
using QuillKit.Models;

namespace QuillKit.Services;

public class SitemapService
{
    private readonly IPostService _postService;
    private readonly SiteConfigService _siteConfigService;

    public SitemapService(IPostService postService, SiteConfigService siteConfigService)
    {
        _postService = postService;
        _siteConfigService = siteConfigService;
    }

    /// <summary>
    /// 🗺️ Generate XML sitemap for search engines
    /// </summary>
    public async Task<string> GenerateSitemapAsync()
    {
        var config = _siteConfigService.Config;
        var baseUrl = GetBaseUrl(config.BaseUrl);
        
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        // Add home page
        AddUrl(sb, baseUrl, DateTime.UtcNow, "daily", "1.0");

        // Add all published posts
        var posts = await _postService.GetAllPostsAsync();
        var publishedPosts = posts
            .Where(p => p.Status == PostStatus.Published)
            .OrderByDescending(p => p.PubDate)
            .ToList();

        foreach (var post in publishedPosts)
        {
            var url = post.Type == PostType.Post 
                ? $"{baseUrl}/post/{post.Slug}" 
                : $"{baseUrl}/page/{post.Slug}";
            
            var lastMod = post.LastModified;
            var changeFreq = post.Type == PostType.Post ? "monthly" : "yearly";
            var priority = post.Type == PostType.Post ? "0.8" : "0.6";
            
            AddUrl(sb, url, lastMod, changeFreq, priority);
        }

        // Add category pages
        var categories = publishedPosts
            .Where(p => p.Type == PostType.Post)
            .SelectMany(p => p.Categories ?? new List<string>())
            .Distinct()
            .ToList();

        foreach (var category in categories)
        {
            var url = $"{baseUrl}/category/{Uri.EscapeDataString(category)}";
            AddUrl(sb, url, DateTime.UtcNow, "weekly", "0.5");
        }

        // Add tag pages
        var tags = publishedPosts
            .Where(p => p.Type == PostType.Post)
            .SelectMany(p => p.Tags ?? new List<string>())
            .Distinct()
            .ToList();

        foreach (var tag in tags)
        {
            var url = $"{baseUrl}/tag/{Uri.EscapeDataString(tag)}";
            AddUrl(sb, url, DateTime.UtcNow, "weekly", "0.4");
        }

        sb.AppendLine("</urlset>");
        return sb.ToString();
    }

    private void AddUrl(StringBuilder sb, string url, DateTime lastMod, string changeFreq, string priority)
    {
        sb.AppendLine("  <url>");
        sb.AppendLine($"    <loc>{XmlEscape(url)}</loc>");
        sb.AppendLine($"    <lastmod>{lastMod:yyyy-MM-dd}</lastmod>");
        sb.AppendLine($"    <changefreq>{changeFreq}</changefreq>");
        sb.AppendLine($"    <priority>{priority}</priority>");
        sb.AppendLine("  </url>");
    }

    private string GetBaseUrl(string configBaseUrl)
    {
        // Use configured base URL or fall back to placeholder
        if (string.IsNullOrWhiteSpace(configBaseUrl) || configBaseUrl == "/")
        {
            return "https://yoursite.com"; // This will be replaced at runtime
        }
        return configBaseUrl.TrimEnd('/');
    }

    private string XmlEscape(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}
