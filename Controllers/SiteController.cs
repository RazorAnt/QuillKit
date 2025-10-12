using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QuillKit.Models;
using QuillKit.Models.ViewModels;
using QuillKit.Services;

namespace QuillKit.Controllers;

public class SiteController : Controller
{
    private readonly IPostService _postService;
    private readonly SiteConfigService _siteConfigService;
    private readonly SyndicationService _syndicationService;
    private readonly SitemapService _sitemapService;
    private readonly RobotsService _robotsService;
    private readonly ILogger<SiteController> _logger;

    public SiteController(IPostService postService, SiteConfigService siteConfigService, SyndicationService syndicationService, SitemapService sitemapService, RobotsService robotsService, ILogger<SiteController> logger)
    {
        _postService = postService;
        _siteConfigService = siteConfigService;
        _syndicationService = syndicationService;
        _sitemapService = sitemapService;
        _robotsService = robotsService;
        _logger = logger;
    }

    /// <summary>
    /// 🏠 Main page showing recent posts
    /// </summary>
    public async Task<IActionResult> Index(int page = 1)
    {
        var pageSize = _siteConfigService.Config.PostsPerPage;
        
        // 🔐 Check if user is authenticated to show drafts
        var isAdmin = HttpContext.Session.GetString("IsAdmin") == "true";

        // Get paginated posts and total count (include drafts if admin)
        var posts = await _postService.GetPublishedPostsAsync(page, pageSize, includeDrafts: isAdmin);
        var totalPosts = await _postService.GetPublishedPostsCountAsync(includeDrafts: isAdmin);
        var totalPages = (int)Math.Ceiling((double)totalPosts / pageSize);

        // Create paginated view model
        var paginatedPosts = new PaginatedViewModel<Post>(posts, page, totalPages, totalPosts, pageSize);

        SetSEOViewData("Home", "");

        return View(paginatedPosts);
    }

    /// <summary>
    /// 📖 Display a single post by slug
    /// </summary>
    [Route("post/{slug}")]
    public async Task<IActionResult> Post(string slug)
    {
        if (string.IsNullOrEmpty(slug))
        {
            return NotFound();
        }

        // 🔐 Check if user is authenticated to show drafts
        var isAdmin = HttpContext.Session.GetString("IsAdmin") == "true";
        var post = await _postService.GetPostBySlugAsync(slug, includeDrafts: isAdmin);

        if (post == null)
        {
            _logger.LogWarning("Post not found: {Slug}", slug);
            return NotFound();
        }

        // Allow viewing drafts only if admin, otherwise must be published
        if (!isAdmin && (post.Status != PostStatus.Published || post.Type != PostType.Post))
        {
            _logger.LogWarning("Attempted to access unpublished post: {Slug}", slug);
            return NotFound();
        }
        
        // Ensure it's actually a post type
        if (post.Type != PostType.Post)
        {
            _logger.LogWarning("Attempted to access non-post content as post: {Slug}", slug);
            return NotFound();
        }

        SetSEOViewData(post);

        return View(post);
    }

    /// <summary>
    /// 📄 Display a single page by slug
    /// </summary>
    [Route("page/{slug}")]
    public async Task<IActionResult> Page(string slug)
    {
        if (string.IsNullOrEmpty(slug))
        {
            return NotFound();
        }

        var page = await _postService.GetPostBySlugAsync(slug);

        if (page == null)
        {
            _logger.LogWarning("Page not found: {Slug}", slug);
            return NotFound();
        }

        if (page.Status != PostStatus.Published || page.Type != PostType.Page)
        {
            _logger.LogWarning("Attempted to access unpublished page: {Slug}", slug);
            return NotFound();
        }

        SetSEOViewData(page);

        return View(page);
    }

    /// <summary>
    /// � Category page showing posts in a specific category
    /// </summary>
    [Route("category/{category}")]
    public async Task<IActionResult> Category(string category, int page = 1)
    {
        if (string.IsNullOrEmpty(category))
        {
            return NotFound();
        }

        var pageSize = _siteConfigService.Config.PostsPerPage;

        // Get all posts and filter by category
        var allPosts = await _postService.GetAllPostsAsync();
        var categoryPosts = allPosts
            .Where(p => p.Status == PostStatus.Published && p.Type == PostType.Post)
            .Where(p => p.Categories.Any(c => c.Equals(category, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(p => p.PubDate)
            .ToList();

        // Apply pagination
        var totalPosts = categoryPosts.Count;
        var totalPages = (int)Math.Ceiling((double)totalPosts / pageSize);
        var paginatedPosts = categoryPosts
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Create paginated view model
        var paginatedViewModel = new PaginatedViewModel<Post>(paginatedPosts, page, totalPages, totalPosts, pageSize);

        ViewData["CategoryName"] = category;
        ViewData["PostCount"] = totalPosts;

        SetSEOViewData($"Category: {category}", $"category/{category}");

        // Use the same Index view but with filtered posts
        return View("Index", paginatedViewModel);
    }

    /// <summary>
    /// 🏷️ Tag page showing posts with a specific tag
    /// </summary>
    [Route("tag/{tag}")]
    public async Task<IActionResult> Tag(string tag, int page = 1)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return NotFound();
        }

        var pageSize = _siteConfigService.Config.PostsPerPage;
        
        // 🔐 Check if user is authenticated to show drafts
        var isAdmin = HttpContext.Session.GetString("IsAdmin") == "true";

        // Get all posts and filter by tag
        var allPosts = await _postService.GetAllPostsAsync();
        var query = allPosts.Where(p => p.Type == PostType.Post);
        
        // Include drafts only if admin
        if (!isAdmin)
        {
            query = query.Where(p => p.Status == PostStatus.Published);
        }
        
        var tagPosts = query
            .Where(p => p.Tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(p => p.PubDate)
            .ToList();

        // Apply pagination
        var totalPosts = tagPosts.Count;
        var totalPages = (int)Math.Ceiling((double)totalPosts / pageSize);
        var paginatedPosts = tagPosts
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Create paginated view model
        var paginatedViewModel = new PaginatedViewModel<Post>(paginatedPosts, page, totalPages, totalPosts, pageSize);

        ViewData["TagName"] = tag;
        ViewData["PostCount"] = totalPosts;

        SetSEOViewData($"Tag: {tag}", $"tag/{tag}");

        // Use the same Index view but with filtered posts
        return View("Index", paginatedViewModel);
    }

    /// <summary>
    /// 🔍 Search results page
    /// </summary>
    [Route("search")]
    public async Task<IActionResult> Search(string q, int page = 1)
    {
        if (string.IsNullOrEmpty(q))
        {
            ViewData["Query"] = "";
            ViewData["ResultCount"] = 0;
            SetSEOViewData("Search", "/search");
            return View(new List<Post>());
        }

        var searchResults = await _postService.SearchPostsAsync(q);

        ViewData["Query"] = q;
        ViewData["ResultCount"] = searchResults.Count;

        SetSEOViewData($"Search: {q}", $"/search?q={Uri.EscapeDataString(q)}");
        return View(searchResults);
    }

    /// <summary>
    /// 📡 RSS feed endpoint
    /// </summary>
    [Route("rss")]
    [Route("feed")]
    [Route("feed/rss")]
    public async Task<IActionResult> Rss()
    {
        var posts = await _postService.GetPublishedPostsAsync(1, 20);
        var siteConfig = _siteConfigService.Config;

        var rssXml = _syndicationService.GenerateRssFeed(posts, siteConfig);

        Response.ContentType = "application/rss+xml; charset=utf-8";
        Response.Headers["Cache-Control"] = "public, max-age=3600"; // Cache for 1 hour

        return Content(rssXml, "application/rss+xml");
    }

    /// <summary>
    /// 🗺️ XML Sitemap endpoint
    /// </summary>
    [Route("sitemap.xml")]
    public async Task<IActionResult> Sitemap()
    {
        var sitemapXml = await _sitemapService.GenerateSitemapAsync();

        Response.ContentType = "application/xml; charset=utf-8";
        Response.Headers["Cache-Control"] = "public, max-age=86400"; // Cache for 24 hours

        return Content(sitemapXml, "application/xml");
    }

    /// <summary>
    /// 🤖 Dynamic robots.txt endpoint
    /// </summary>
    [Route("robots.txt")]
    public IActionResult Robots()
    {
        var robotsTxt = _robotsService.GenerateRobotsTxt();

        Response.ContentType = "text/plain; charset=utf-8";
        Response.Headers["Cache-Control"] = "public, max-age=86400"; // Cache for 24 hours

        return Content(robotsTxt, "text/plain");
    }

    /// <summary>
    ///  Atom feed endpoint
    /// </summary>
    [Route("atom")]
    [Route("feed/atom")]
    public async Task<IActionResult> Atom()
    {
        // TODO: Implement Atom feed generation
        var posts = await _postService.GetPublishedPostsAsync(1, 20);

        // Placeholder - will need to generate actual Atom XML
        Response.ContentType = "application/atom+xml";
        return Content("<?xml version=\"1.0\"?><feed xmlns=\"http://www.w3.org/2005/Atom\"><title>Site Atom</title></feed>");
    }

    /// <summary>
    /// ❌ Error page for unhandled exceptions
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private void SetSEOViewData(string title, string slug)
    {
        var post = new Post { Title = title, Slug = slug, Type = PostType.Page };
        SetSEOViewData(post);
    }

    private void SetSEOViewData(Post post)
    {
        var config = _siteConfigService.Config;
        ViewData["Title"] = post.Title;
        ViewData["FullTitle"] = $"{post.Title} | {config.Title}";

        // Build proper base URL - fall back to request URL if config is incomplete
        var baseUrl = config.BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl) || baseUrl == "/")
        {
            baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
        }
        baseUrl = baseUrl.TrimEnd('/');
        
        var cleanSlug = post.Slug.TrimStart('/');
        ViewData["CanonicalUrl"] = string.IsNullOrEmpty(cleanSlug) ? baseUrl : $"{baseUrl}/{cleanSlug}";

        // Description: prefer post.Description, then Excerpt, then site description
        string? description = post.Description;
        if (string.IsNullOrWhiteSpace(description))
            description = post.Excerpt;
        if (string.IsNullOrWhiteSpace(description))
            description = config.Description ?? "";
        ViewData["Description"] = description;

        // Combine config keywords with post tags/categories
        var allKeywords = new List<string>();
        if (!string.IsNullOrEmpty(config.Keywords))
        {
            allKeywords.AddRange(config.Keywords.Split(',').Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k)));
        }
        if (post.Tags != null)
            allKeywords.AddRange(post.Tags);
        if (post.Categories != null)
            allKeywords.AddRange(post.Categories);
        ViewData["Keywords"] = allKeywords.Any() ? string.Join(", ", allKeywords.Distinct()) : "";

        // Open Graph & Twitter
        if (!string.IsNullOrEmpty(post.Image))
        {
            // Handle both relative and absolute image URLs
            if (post.Image.StartsWith("http"))
            {
                ViewData["OgImage"] = post.Image;
            }
            else
            {
                ViewData["OgImage"] = $"{baseUrl}{(post.Image.StartsWith('/') ? post.Image : '/' + post.Image)}";
            }
            ViewData["TwitterCard"] = "summary_large_image";
        }
        else
        {
            ViewData["TwitterCard"] = "summary";
        }

        // Set OG type
        ViewData["OgType"] = post.Type == PostType.Post ? "article" : "website";
    }

}
