using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Filters;
using QuillKit.Models;
using QuillKit.Services;

namespace QuillKit.Controllers;

public class AdminController : Controller
{
    private readonly IPostService _postService;
    private readonly ILogger<AdminController> _logger;
    private readonly SiteConfigService _siteConfigService;
    private readonly IConfiguration _configuration;

    public AdminController(IPostService postService, ILogger<AdminController> logger, SiteConfigService siteConfigService, IConfiguration configuration)
    {
        _postService = postService;
        _logger = logger;
        _siteConfigService = siteConfigService;
        _configuration = configuration;
    }
    
        /// <summary>
    /// 🔐 Admin login page (GET)
    /// </summary>
    [HttpGet]
    [Route("admin/login")]
    [AllowAnonymous]
    public IActionResult Login()
    {
        return View();
    }

    /// <summary>
    /// 🔐 Admin login page (POST)
    /// </summary>
    [HttpPost]
    [Route("admin/login")]
    [AllowAnonymous]
    public IActionResult Login(string username, string password)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .Build();
        var adminSection = config.GetSection("AdminAuth");
        var storedUsername = adminSection["Username"];
        var storedHash = adminSection["PasswordHash"];
        if (string.IsNullOrWhiteSpace(storedUsername) || string.IsNullOrWhiteSpace(storedHash))
        {
            ViewData["LoginFailed"] = true;
            return View();
        }
        if (username == storedUsername && VerifyPasswordHash(password, storedHash))
        {
            // Authenticate user (simple session cookie)
            HttpContext.Session.SetString("IsAdmin", "true");
            return RedirectToAction("Index");
        }
        ViewData["LoginFailed"] = true;
        return View();
    }

    private bool VerifyPasswordHash(string password, string storedHash)
    {
        // For demo: use SHA256. Replace with a stronger hash if needed.
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        return hashString == storedHash.ToLowerInvariant();
    }
    // Restrict all admin actions except login to authenticated users
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var path = context.HttpContext.Request.Path.Value?.ToLower();
        if (path != null && !path.Contains("/admin/login"))
        {
            var isAdmin = context.HttpContext.Session.GetString("IsAdmin");
            if (isAdmin != "true")
            {
                context.Result = Redirect("/admin/login");
            }
        }
        base.OnActionExecuting(context);
    }

    /// <summary>
    /// 🏠 Admin dashboard - overview and quick actions
    /// </summary>
    public async Task<IActionResult> Index()
    {
        // TODO: Implement admin dashboard with overview stats
        var allPosts = await _postService.GetAllPostsAsync();

        var publishedPosts = allPosts.Count(p => p.Type == PostType.Post && p.Status == PostStatus.Published);
        var draftPosts = allPosts.Count(p => p.Type == PostType.Post && p.Status == PostStatus.Draft);

        var publishedPages = allPosts.Count(p => p.Type == PostType.Page && p.Status == PostStatus.Published);
        var draftPages = allPosts.Count(p => p.Type == PostType.Page && p.Status == PostStatus.Draft);

        ViewData["PublishedPosts"] = publishedPosts;
        ViewData["DraftPosts"] = draftPosts;
        ViewData["PublishedPages"] = publishedPages;
        ViewData["DraftPages"] = draftPages;

        // Expose ContentProvider and BaseUrl
        ViewData["ContentProvider"] = _configuration.GetValue<string>("ContentProvider", "Local");
        ViewData["BaseUrl"] = _siteConfigService.Config.BaseUrl;

        return View(allPosts);
    }

    /// <summary>
    /// 🚪 Logout the admin user and redirect to the home page
    /// </summary>
    [HttpGet]
    [Route("admin/logout")]
    public IActionResult Logout()
    {
        // Clear admin flag from session
        try
        {
            HttpContext.Session.Remove("IsAdmin");
        }
        catch
        {
            // ignore session errors
        }

        return Redirect("/");
    }

    /// <summary>
    /// ✏️ Admin editor - create/edit posts and pages
    /// </summary>
    [Route("admin/editor")]
    [Route("admin/editor/{slug?}")]
    public async Task<IActionResult> Editor(string? slug = null)
    {
        Post? post = null;
        
        if (!string.IsNullOrEmpty(slug))
        {
            post = await _postService.GetPostBySlugAsync(slug);
            if (post == null)
            {
                _logger.LogWarning("Post not found for editing: {Slug}", slug);
                return NotFound();
            }
        }
        
        return View(post);
    }

    /// <summary>
    /// 💾 Save post from editor
    /// </summary>
    [HttpPost]
    [Route("admin/editor")]
    public async Task<IActionResult> SavePost(Post post)
    {
        // Parse additional form fields (CategoriesCsv, TagsCsv) as they aren't bound to complex types by default
        try
        {
            var categoriesCsv = Request.Form["CategoriesCsv"].FirstOrDefault() ?? string.Empty;
            var tagsCsv = Request.Form["TagsCsv"].FirstOrDefault() ?? string.Empty;

            post.Categories = categoriesCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

            post.Tags = tagsCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

            // Parse PubDate if provided as a date input
            var pubDateStr = Request.Form["PubDate"].FirstOrDefault();
            if (!string.IsNullOrEmpty(pubDateStr) && DateTime.TryParse(pubDateStr, out var parsedDate))
            {
                post.PubDate = parsedDate;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing form fields for post save");
        }

        if (!ModelState.IsValid)
        {
            return View("Editor", post);
        }

        // Ensure FileName is preserved when editing an existing file
        if (!string.IsNullOrEmpty(Request.Form["FileName"]))
        {
            post.FileName = Request.Form["FileName"].FirstOrDefault() ?? post.FileName;
        }

        try
        {
            await _postService.SavePostAsync(post);
            _logger.LogInformation("Post saved: {Title}", post.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save post: {Title}", post.Title);
            ModelState.AddModelError("", $"Failed to save post: {ex.Message}");
            return View("Editor", post);
        }

        // Redirect to appropriate list based on post type and status
        return post.Status == PostStatus.Draft 
            ? RedirectToAction("DraftList")
            : post.Type == PostType.Page 
                ? RedirectToAction("PageList") 
                : RedirectToAction("PostList");
    }

    /// <summary>
    /// 📚 Admin post list - manage all posts
    /// </summary>
    [Route("admin/posts")]
    public async Task<IActionResult> PostList(string? filter = null)
    {
        var allPosts = await _postService.GetAllPostsAsync();
        var posts = allPosts.Where(p => p.Type == PostType.Post)
                            .OrderByDescending(p => p.PubDate)
                            .ToList();

        return View(posts);
    }

    /// <summary>
    /// 📚 Admin drafts list - manage all drafts
    /// </summary>
    [Route("admin/drafts")]
    public async Task<IActionResult> DraftList(string? filter = null)
    {
        var allPosts = await _postService.GetAllPostsAsync();
        var drafts = allPosts.Where(p => p.Status == PostStatus.Draft)
                            .OrderBy(p => p.Title)
                            .ToList();

        return View("PostList", drafts);
    }

    /// <summary>
    /// � Admin page list - manage all pages
    /// </summary>
    [Route("admin/pages")]
    public async Task<IActionResult> PageList()
    {
        var allPosts = await _postService.GetAllPostsAsync();
        var pages = allPosts.Where(p => p.Type == PostType.Page)
                            .OrderBy(p => p.Title)
                            .ToList();

        return View("PostList", pages);
    }

    /// <summary>
    /// �🗑️ Delete post
    /// </summary>
    [HttpPost]
    [Route("admin/delete/{slug}")]
    public async Task<IActionResult> DeletePost(string slug)
    {
        // TODO: Not started
        // TODO: Add confirmation and proper error handling
        await _postService.DeletePostAsync(slug);
        _logger.LogInformation("Post deleted: {Slug}", slug);
        
        return RedirectToAction("PostList");
    }

    /// <summary>
    /// 🖼️ Admin media management - upload/organize images
    /// </summary>
    [Route("admin/media")]
    public async Task<IActionResult> Media()
    {
        // TODO: Implement media management interface
        var mediaFiles = await _postService.GetMediaFilesAsync();
        return View(mediaFiles);
    }

    /// <summary>
    /// 📤 Upload media file
    /// </summary>
    [HttpPost]
    [Route("admin/media/upload")]
    public Task<IActionResult> UploadMedia(IFormFile file)
    {
        // TODO: Implement file upload with validation and security
        if (file == null || file.Length == 0)
        {
            return Task.FromResult<IActionResult>(BadRequest("No file selected"));
        }
        
        // Placeholder for file upload logic
        _logger.LogInformation("File upload attempted: {FileName}", file.FileName);
        
        return Task.FromResult<IActionResult>(RedirectToAction("Media"));
    }

    /// <summary>
    /// ⚙️ Admin settings - blog configuration
    /// </summary>
    [Route("admin/settings")]
    public IActionResult Settings()
    {
        // TODO: Implement settings management
        return View();
    }

    /// <summary>
    /// 📦 Admin export - content backup feature
    /// </summary>
    [Route("admin/export")]
    public async Task<IActionResult> Export()
    {
        // TODO: Implement content export/backup functionality
        var allPosts = await _postService.GetAllPostsAsync();
        
        // Placeholder for export logic
        _logger.LogInformation("Content export requested - {PostCount} posts", allPosts.Count);
        
        return View();
    }

    /// <summary>
    /// 📥 Download backup file
    /// </summary>
    [HttpPost]
    [Route("admin/backup")]
    public async Task<IActionResult> DownloadBackup()
    {
        // TODO: Generate and download backup file
        // zip file should contain all md files, media and config folders.
        var allPosts = await _postService.GetAllPostsAsync();
        
        // Placeholder - will need to create actual backup file
        var backupContent = $"Backup generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\nTotal posts: {allPosts.Count}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(backupContent);
        
        return File(bytes, "application/zip", $"quillkit-backup-{DateTime.UtcNow:yyyyMMdd}.zip");
    }

    // [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    // public IActionResult Error()
    // {
    //     return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    // }
}
