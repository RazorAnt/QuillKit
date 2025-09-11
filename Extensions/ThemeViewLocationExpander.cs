using Microsoft.AspNetCore.Mvc.Razor;
using QuillKit.Models;

namespace QuillKit.Extensions;

/// <summary>
/// 🎨 Enables theme view overrides by adding /Theme/Views to the view search path
/// Works with both local files and Azure Blob Storage
/// </summary>
public class ThemeViewLocationExpander : IViewLocationExpander
{
    private readonly IWebHostEnvironment _environment;
    private readonly ContentProvider _contentProvider;

    public ThemeViewLocationExpander(IWebHostEnvironment environment, ContentProvider contentProvider)
    {
        _environment = environment;
        _contentProvider = contentProvider;
    }

    public void PopulateValues(ViewLocationExpanderContext context)
    {
        // 📍 Add theme identifier if we want to support multiple themes later
        // For now, we'll just use a single theme directory
        context.Values["theme"] = "default";
    }

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        // 🔍 Check if theme views exist based on content provider
        bool themeViewsExist = false;
        
        if (_contentProvider == ContentProvider.Local)
        {
            // Local file system check
            var themeViewsPath = Path.Combine(_environment.ContentRootPath, "Content", "Theme", "Views");
            themeViewsExist = Directory.Exists(themeViewsPath);
        }
        else if (_contentProvider == ContentProvider.AzureBlob)
        {
            // For Azure Blob, we'll assume theme views exist if we have any theme content
            // This is a reasonable assumption since theme is part of the content
            themeViewsExist = true; // TODO: Could implement async check if needed
        }
        
        if (!themeViewsExist)
        {
            // 🚫 No theme directory, return original view locations unchanged
            return viewLocations;
        }

        // ✨ Add theme view locations BEFORE default locations for priority override
        var themeLocations = new[]
        {
            "/Content/Theme/Views/{1}/{0}.cshtml",      // Controller-specific views
            "/Content/Theme/Views/Shared/{0}.cshtml"    // Shared views
        };

        // 🎯 Combine theme locations with original locations (theme takes precedence)
        return themeLocations.Concat(viewLocations);
    }
}
