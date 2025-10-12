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
    private readonly IContentService _contentService;
    private readonly ILogger<AdminController> _logger;
    private readonly SiteConfigService _siteConfigService;
    private readonly IConfiguration _configuration;

    public AdminController(IPostService postService, IContentService contentService, ILogger<AdminController> logger, SiteConfigService siteConfigService, IConfiguration configuration)
    {
        _postService = postService;
        _contentService = contentService;
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
        var parseErrors = await _postService.GetParseErrorsAsync();

        var publishedPosts = allPosts.Count(p => p.Type == PostType.Post && p.Status == PostStatus.Published);
        var draftPosts = allPosts.Count(p => p.Type == PostType.Post && p.Status == PostStatus.Draft);

        var publishedPages = allPosts.Count(p => p.Type == PostType.Page && p.Status == PostStatus.Published);
        var draftPages = allPosts.Count(p => p.Type == PostType.Page && p.Status == PostStatus.Draft);

        ViewData["PublishedPosts"] = publishedPosts;
        ViewData["DraftPosts"] = draftPosts;
        ViewData["PublishedPages"] = publishedPages;
        ViewData["DraftPages"] = draftPages;
        ViewData["ParseErrors"] = parseErrors;

        // Expose ContentProvider and BaseUrl
        ViewData["ContentProvider"] = _configuration.GetValue<string>("ContentProvider", "Local");
        ViewData["BaseUrl"] = _siteConfigService.Config.BaseUrl;

        return View(allPosts);
    }

    /// <summary>
    /// � Reload all posts and pages from storage
    /// </summary>
    [HttpPost]
    [Route("admin/reload")]
    public async Task<IActionResult> ReloadData()
    {
        // 🔄 Reload all posts and pages from content service
        try
        {
            await _postService.ReloadPostsAsync();
            await _siteConfigService.ReloadConfigAsync();
            
            _logger.LogInformation("✅ Data reloaded successfully from admin dashboard");
            TempData["SuccessMessage"] = "All data reloaded successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to reload data");
            TempData["ErrorMessage"] = $"Failed to reload data: {ex.Message}";
        }
        
        return RedirectToAction("Index");
    }

    /// <summary>
    /// �🚪 Logout the admin user and redirect to the home page
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
        _logger.LogInformation("💾 SavePost called - Title: {Title}, Type: {Type}, Status: {Status}, FileName: {FileName}", 
            post.Title, post.Type, post.Status, post.FileName);
        
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

        // 🧹 Clean up placeholder values before validation
        if (string.Equals(post.Link, "none", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("🧹 Cleaning up Link field: 'None' -> empty string");
            post.Link = string.Empty;
        }
        
        if (string.Equals(post.Image, "none", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("🧹 Cleaning up Image field: 'None' -> empty string");
            post.Image = string.Empty;
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("❌ ModelState is invalid! Errors:");
            foreach (var key in ModelState.Keys)
            {
                var errors = ModelState[key]?.Errors;
                if (errors != null && errors.Count > 0)
                {
                    foreach (var error in errors)
                    {
                        _logger.LogWarning("  - {Key}: {Error}", key, error.ErrorMessage);
                    }
                }
            }
            return View("Editor", post);
        }

        _logger.LogInformation("✅ ModelState is valid, proceeding with save...");

        // Ensure FileName is preserved when editing an existing file
        if (!string.IsNullOrEmpty(Request.Form["FileName"]))
        {
            post.FileName = Request.Form["FileName"].FirstOrDefault() ?? post.FileName;
        }

        try
        {
            _logger.LogInformation("📞 Calling SavePostAsync...");
            await _postService.SavePostAsync(post);
            _logger.LogInformation("✅ Post saved: {Title}", post.Title);
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
        var mediaFiles = await _postService.GetMediaFilesAsync();
        _logger.LogInformation("📊 Controller received {Count} media files to pass to view", mediaFiles.Count);
        foreach (var file in mediaFiles.Take(5))
        {
            _logger.LogInformation("  📁 Sample file in controller: {File}", file);
        }
        return View(mediaFiles);
    }

    /// <summary>
    /// 📤 Upload media file
    /// </summary>
    [HttpPost]
    [Route("admin/media/upload")]
    public async Task<IActionResult> UploadMedia(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("", "No file selected");
            return RedirectToAction("Media");
        }

        try
        {
            var fileName = Path.GetFileName(file.FileName);
            var relativePath = $"media/{fileName}";
            
            // Check if file already exists
            if (await _contentService.FileExistsAsync(relativePath))
            {
                ModelState.AddModelError("", $"File '{fileName}' already exists");
                return RedirectToAction("Media");
            }
            
            // Read file content into memory
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();
            
            // For IContentService, we need to write the file
            // Note: IContentService currently only has WriteFileAsync for text
            // We need to handle binary files differently based on provider
            var contentProvider = _configuration.GetValue<string>("ContentProvider", "Local");
            
            if (contentProvider == "AzureBlob")
            {
                // Upload directly to Azure Blob using BlobServiceClient
                var connectionString = _configuration.GetConnectionString("AzureStorage");
                var blobServiceClient = new Azure.Storage.Blobs.BlobServiceClient(connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient("content");
                await containerClient.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.None);
                
                var blobClient = containerClient.GetBlobClient(relativePath);
                using var uploadStream = new MemoryStream(fileBytes);
                await blobClient.UploadAsync(uploadStream, overwrite: false);
            }
            else
            {
                // Local file system
                var mediaPath = Path.Combine(Directory.GetCurrentDirectory(), "Content", "media");
                Directory.CreateDirectory(mediaPath);
                var filePath = Path.Combine(mediaPath, fileName);
                await System.IO.File.WriteAllBytesAsync(filePath, fileBytes);
            }
            
            _logger.LogInformation("Media file uploaded: {FileName}", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload media file: {FileName}", file.FileName);
            ModelState.AddModelError("", $"Failed to upload file: {ex.Message}");
        }
        
        return RedirectToAction("Media");
    }

    /// <summary>
    /// 🗑️ Delete media file
    /// </summary>
    [HttpPost]
    [Route("admin/media/delete")]
    public async Task<IActionResult> DeleteMedia(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return BadRequest("No file specified");
        }

        try
        {
            var relativePath = $"media/{fileName}";
            
            // Check if file exists before trying to delete
            if (await _contentService.FileExistsAsync(relativePath))
            {
                await _contentService.DeleteFileAsync(relativePath);
                _logger.LogInformation("Media file deleted: {FileName}", fileName);
            }
            else
            {
                _logger.LogWarning("Media file not found for deletion: {FileName}", fileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete media file: {FileName}", fileName);
            ModelState.AddModelError("", $"Failed to delete file: {ex.Message}");
        }
        
        return RedirectToAction("Media");
    }

    /// <summary>
    /// ⚙️ Admin settings - blog configuration
    /// </summary>
    [Route("admin/settings")]
    public async Task<IActionResult> Settings()
    {
        try
        {
            var configContent = await _contentService.ReadFileAsync("Config/site-config.yml");
            return View((object)configContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load site-config.yml");
            ModelState.AddModelError("", "Failed to load configuration file");
            return View((object)string.Empty);
        }
    }

    /// <summary>
    /// 💾 Save settings configuration
    /// </summary>
    [HttpPost]
    [Route("admin/settings")]
    public async Task<IActionResult> SaveSettings(string configContent)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(configContent))
            {
                ModelState.AddModelError("", "Configuration content cannot be empty");
                return View("Settings", (object)configContent);
            }

            await _contentService.WriteFileAsync("Config/site-config.yml", configContent);
            
            // Reload the site configuration
            await _siteConfigService.ReloadConfigAsync();
            
            _logger.LogInformation("Site configuration saved and reloaded");
            TempData["SuccessMessage"] = "Configuration saved successfully!";
            
            return RedirectToAction("Settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save site-config.yml");
            ModelState.AddModelError("", $"Failed to save configuration: {ex.Message}");
            return View("Settings", (object)configContent);
        }
    }

    /// <summary>
    /// � Raw file editor - load file content
    /// </summary>
    [Route("admin/raw-editor")]
    public async Task<IActionResult> RawEditor(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            TempData["ErrorMessage"] = "File name is required";
            return RedirectToAction("Index");
        }

        try
        {
            var content = await _postService.GetRawFileContentAsync(fileName);
            
            if (content == null)
            {
                TempData["ErrorMessage"] = $"File not found: {fileName}";
                return RedirectToAction("Index");
            }

            ViewData["FileName"] = fileName;
            return View((object)content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load file for raw editing: {FileName}", fileName);
            TempData["ErrorMessage"] = $"Failed to load file: {ex.Message}";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// 💾 Save raw file content
    /// </summary>
    [HttpPost]
    [Route("admin/raw-editor")]
    public async Task<IActionResult> SaveRawFile(string fileName, string fileContent)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            TempData["ErrorMessage"] = "File name is required";
            return RedirectToAction("Index");
        }

        try
        {
            if (string.IsNullOrWhiteSpace(fileContent))
            {
                ModelState.AddModelError("", "File content cannot be empty");
                ViewData["FileName"] = fileName;
                return View("RawEditor", (object)fileContent);
            }

            await _postService.SaveRawFileAsync(fileName, fileContent);
            
            _logger.LogInformation("Raw file saved and posts reloaded: {FileName}", fileName);
            TempData["SuccessMessage"] = $"File '{fileName}' saved successfully!";
            
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save raw file: {FileName}", fileName);
            ModelState.AddModelError("", $"Failed to save file: {ex.Message}");
            ViewData["FileName"] = fileName;
            return View("RawEditor", (object)fileContent);
        }
    }

    /// <summary>
    /// �📦 Admin backup page
    /// </summary>
    [Route("admin/backup")]
    public IActionResult Backup()
    {
        // 📦 Display backup page with download button
        return View();
    }

    /// <summary>
    /// 📥 Download backup file - creates zip with all content
    /// </summary>
    [HttpPost]
    [Route("admin/backup/download")]
    public async Task<IActionResult> DownloadBackup()
    {
        // 📦 Generate and download backup file in memory
        try
        {
            _logger.LogInformation("📦 Generating backup...");
            
            using var memoryStream = new MemoryStream();
            using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
            {
                // Get all files from content service
                var allFiles = await _contentService.ListFilesAsync("", "*.*");
                
                foreach (var file in allFiles)
                {
                    try
                    {
                        // Read file content
                        var content = await _contentService.ReadFileAsync(file);
                        
                        // Add to zip with proper path structure
                        var entry = archive.CreateEntry(file, System.IO.Compression.CompressionLevel.Optimal);
                        using var entryStream = entry.Open();
                        using var writer = new StreamWriter(entryStream);
                        await writer.WriteAsync(content);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Could not backup file: {FileName}", file);
                        // Continue with other files
                    }
                }
                
                // Add media files (binary files need special handling)
                var mediaFiles = await _postService.GetMediaFilesAsync();
                foreach (var mediaFile in mediaFiles)
                {
                    try
                    {
                        var relativePath = $"media/{mediaFile}";
                        
                        // For binary files, we need to read as bytes
                        // We'll use a different approach based on provider
                        var contentProvider = _configuration.GetValue<string>("ContentProvider", "Local");
                        
                        if (contentProvider == "AzureBlob")
                        {
                            var connectionString = _configuration.GetConnectionString("AzureStorage");
                            var blobServiceClient = new Azure.Storage.Blobs.BlobServiceClient(connectionString);
                            var containerClient = blobServiceClient.GetBlobContainerClient("content");
                            var blobClient = containerClient.GetBlobClient(relativePath);
                            
                            if (await blobClient.ExistsAsync())
                            {
                                var entry = archive.CreateEntry(relativePath, System.IO.Compression.CompressionLevel.Optimal);
                                using var entryStream = entry.Open();
                                await blobClient.DownloadToAsync(entryStream);
                            }
                        }
                        else
                        {
                            // Local file system
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Content", "media", mediaFile);
                            if (System.IO.File.Exists(filePath))
                            {
                                var entry = archive.CreateEntry(relativePath, System.IO.Compression.CompressionLevel.Optimal);
                                using var entryStream = entry.Open();
                                using var fileStream = System.IO.File.OpenRead(filePath);
                                await fileStream.CopyToAsync(entryStream);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Could not backup media file: {FileName}", mediaFile);
                        // Continue with other files
                    }
                }
            }
            
            memoryStream.Position = 0;
            var fileBytes = memoryStream.ToArray();
            
            var fileName = $"quillkit-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
            _logger.LogInformation("✅ Backup generated: {FileName} ({Size} bytes)", fileName, fileBytes.Length);
            
            return File(fileBytes, "application/zip", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to generate backup");
            TempData["ErrorMessage"] = $"Failed to generate backup: {ex.Message}";
            return RedirectToAction("Backup");
        }
    }

    // [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    // public IActionResult Error()
    // {
    //     return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    // }
}
