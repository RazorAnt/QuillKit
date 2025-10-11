using System.Text;

namespace QuillKit.Services;

public class RobotsService
{
    private readonly SiteConfigService _siteConfigService;

    public RobotsService(SiteConfigService siteConfigService)
    {
        _siteConfigService = siteConfigService;
    }

    /// <summary>
    /// 🤖 Generate robots.txt content dynamically using site configuration
    /// </summary>
    public string GenerateRobotsTxt()
    {
        var baseUrl = _siteConfigService.Config.BaseUrl?.TrimEnd('/') ?? "https://yoursite.com";
        
        var sb = new StringBuilder();
        sb.AppendLine("# robots.txt for QuillKit");
        sb.AppendLine("# This file tells search engines how to crawl this site");
        sb.AppendLine();
        sb.AppendLine("# Allow all well-behaved search engines to crawl everything");
        sb.AppendLine("User-agent: *");
        sb.AppendLine("Allow: /");
        sb.AppendLine();
        sb.AppendLine("# Block crawling of content source files and private areas");
        sb.AppendLine("Disallow: /Content/");
        sb.AppendLine("Disallow: /Private/");
        sb.AppendLine("Disallow: /bin/");
        sb.AppendLine("Disallow: /obj/");
        sb.AppendLine();
        sb.AppendLine("# Block crawling of development/debug files");
        sb.AppendLine("Disallow: *.log");
        sb.AppendLine("Disallow: *.tmp");
        sb.AppendLine();
        sb.AppendLine("# Point to sitemap");
        sb.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");
        sb.AppendLine();
        sb.AppendLine("# Crawl delay (be nice to servers)");
        sb.AppendLine("Crawl-delay: 1");
        
        return sb.ToString();
    }
}
