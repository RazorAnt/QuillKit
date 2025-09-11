using Microsoft.AspNetCore.Mvc.Razor;

namespace QuillKit.Extensions;

/// <summary>
/// 🎨 Enables theme view overrides by adding /Theme/Views to the view search path
/// Theme views are always served locally for optimal performance
/// </summary>
public class ThemeViewLocationExpander : IViewLocationExpander
{
    private readonly IWebHostEnvironment _environment;

    public ThemeViewLocationExpander(IWebHostEnvironment environment)
    {
        _environment = environment;
        Console.WriteLine($"🎨 ThemeViewLocationExpander created for local theme views");
    }

    public void PopulateValues(ViewLocationExpanderContext context)
    {
        // 📍 Add theme identifier if we want to support multiple themes later
        // For now, we'll just use a single theme directory
        context.Values["theme"] = "default";
    }

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        // 🔍 Check if local theme views exist
        var themeViewsPath = Path.Combine(_environment.ContentRootPath, "Content", "Theme", "Views");
        var themeViewsExist = Directory.Exists(themeViewsPath);
        
        Console.WriteLine($"🔍 Local theme check: {themeViewsPath} exists: {themeViewsExist}");
        
        if (!themeViewsExist)
        {
            // 🚫 No theme directory, return original view locations unchanged
            Console.WriteLine($"🚫 No theme views found, using default locations");
            return viewLocations;
        }

        // ✨ Add theme view locations BEFORE default locations for priority override
        var themeLocations = new[]
        {
            "/Content/Theme/Views/{1}/{0}.cshtml",      // Controller-specific views
            "/Content/Theme/Views/Shared/{0}.cshtml"    // Shared views
        };

        Console.WriteLine($"✨ Adding theme view locations: {string.Join(", ", themeLocations)}");

        // 🎯 Combine theme locations with original locations (theme takes precedence)
        return themeLocations.Concat(viewLocations);
    }
}
