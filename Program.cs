using QuillKit.Services;
using QuillKit.Extensions;
using QuillKit.Models;
using Microsoft.AspNetCore.Mvc.Razor;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 🔄 Parse content provider configuration once
var contentProviderString = builder.Configuration.GetValue<string>("ContentProvider", "Local");
var contentProvider = contentProviderString.ToLowerInvariant() switch
{
    "azureblob" or "azure" => ContentProvider.AzureBlob,
    "local" or "file" or _ => ContentProvider.Local
};

Console.WriteLine($"🔧 Content Provider: {contentProviderString} -> {contentProvider}");

// 🎨 Configure theme view location expansion
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    // Note: We'll configure the view location expander after services are built
    // to avoid BuildServiceProvider issues
});

// Theme views are always served locally for performance

// Configure services during build phase
switch (contentProvider)
{
    case ContentProvider.AzureBlob:
        builder.Services.AddSingleton<IContentService, AzureBlobContentService>();
        builder.Services.AddSingleton<IPostService, FilePostService>();
        break;
    case ContentProvider.Local:
    default:
        builder.Services.AddSingleton<IContentService, LocalFileContentService>();
        builder.Services.AddSingleton<IPostService, FilePostService>();
        break;
}

// Register our other services
builder.Services.AddSingleton<PostParser>();
builder.Services.AddSingleton<SiteConfigService>();
builder.Services.AddSingleton<SyndicationService>();
builder.Services.AddSingleton<SitemapService>();

// 🎨 Configure view location expander now that services are registered
builder.Services.PostConfigure<RazorViewEngineOptions>(options =>
{
    // Clear any existing expanders and add our theme expander
    options.ViewLocationExpanders.Clear();
    options.ViewLocationExpanders.Add(new ThemeViewLocationExpander(builder.Environment));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Site/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Serve static files from wwwroot (CSS, JS, images, etc.)
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

// Configure static file serving based on content provider
switch (contentProvider)
{
    case ContentProvider.AzureBlob:
        // 🌐 Add Azure Blob static file middleware for /media and /assets
        app.UseMiddleware<AzureBlobStaticFileMiddleware>();
        break;
    case ContentProvider.Local:
    default:
        // Serve static files from Content/media folder at /media URL
        var mediaPath = Path.Combine(builder.Environment.ContentRootPath, "Content", "media");
        if (Directory.Exists(mediaPath))
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    Path.Combine(builder.Environment.ContentRootPath, "Content", "media")),
                RequestPath = "/media"
            });
        }

        // Serve static files from Content/theme/assets folder at /assets URL
        var assetPath = Path.Combine(builder.Environment.ContentRootPath, "Content", "Theme", "assets");
        if (Directory.Exists(assetPath))
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    Path.Combine(builder.Environment.ContentRootPath, "Content", "Theme", "assets")),
                RequestPath = "/assets"
            });
        }
        break;
}

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Site}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
