using YamlDotNet.Serialization;

namespace QuillKit.Models;

public class SiteConfig
{
    // Site Settings
    public string Title { get; set; } = "QuillKit";
    public string Subtitle { get; set; } = "Your thoughts, published beautifully";
    public string Description { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string Url { get; set; } = "";

    // Author Information
    public AuthorConfig Author { get; set; } = new();

    // Social Links
    public SocialConfig Social { get; set; } = new();

    // Site Settings
    [YamlMember(Alias = "posts_per_page")]
    public int PostsPerPage { get; set; } = 10;
    
    [YamlMember(Alias = "excerpt_length")]
    public int ExcerptLength { get; set; } = 150;
    
    [YamlMember(Alias = "date_format")]
    public string DateFormat { get; set; } = "MMMM dd, yyyy";
    
    [YamlMember(Alias = "show_excerpts")]
    public bool ShowExcerpts { get; set; } = true;
    
    [YamlMember(Alias = "show_author_bio")]
    public bool ShowAuthorBio { get; set; } = true;
    
    [YamlMember(Alias = "show_related_posts")]
    public bool ShowRelatedPosts { get; set; } = true;
    
    [YamlMember(Alias = "enable_comments")]
    public bool EnableComments { get; set; } = false;

    // Theme Settings - Dynamic dictionary for flexible theme properties
    public Dictionary<string, object> Theme { get; set; } = new();

    // Navigation
    public List<NavigationItem> Navigation { get; set; } = new();

    // Sidebar
    public SidebarConfig Sidebar { get; set; } = new();

    // Content Settings
    public MarkdownConfig Markdown { get; set; } = new();

    // Helper methods for accessing theme properties with type safety and defaults
    public string GetThemeProperty(string key, string defaultValue = "")
    {
        return Theme.ContainsKey(key) && Theme[key] != null 
            ? Theme[key].ToString() ?? defaultValue 
            : defaultValue;
    }

    // public bool GetThemeProperty(string key, bool defaultValue = false)
    // {
    //     if (!Theme.ContainsKey(key) || Theme[key] == null) 
    //         return defaultValue;
        
    //     return Theme[key] switch
    //     {
    //         bool boolValue => boolValue,
    //         string stringValue => bool.TryParse(stringValue, out var result) && result,
    //         _ => defaultValue
    //     };
    // }

    // public int GetThemeProperty(string key, int defaultValue = 0)
    // {
    //     if (!Theme.ContainsKey(key) || Theme[key] == null) 
    //         return defaultValue;
        
    //     return Theme[key] switch
    //     {
    //         int intValue => intValue,
    //         string stringValue => int.TryParse(stringValue, out var result) ? result : defaultValue,
    //         _ => defaultValue
    //     };
    // }

    // Convenience properties for common theme values
    public string PrimaryColor => GetThemeProperty("primary_color", "#EC9C24");
    public string SecondaryColor => GetThemeProperty("secondary_color", "#333333");
    public string AccentColor => GetThemeProperty("accent_color", "#666666");
    public string BackgroundColor => GetThemeProperty("background_color", "#ffffff");
    public string TextColor => GetThemeProperty("text_color", "#333333");
    public string LinkColor => GetThemeProperty("link_color", "#EC9C24");
    public string Instapaper => GetThemeProperty("instapaper", "");
}

public class AuthorConfig
{
    public string Name { get; set; } = "";
    public string Bio { get; set; } = "";
}

public class SocialConfig
{
    public string Email { get; set; } = "";
    public string Feed { get; set; } = "";
    public string GitHub { get; set; } = "";
    public string Bluesky { get; set; } = "";
    public string Twitter { get; set; } = "";
    [YamlMember(Alias = "linkedin")]
    public string LinkedIn { get; set; } = "";
    public string Facebook { get; set; } = "";
    public string Instagram { get; set; } = "";
    [YamlMember(Alias = "youtube")]
    public string YouTube { get; set; } = "";
}

public class FontConfig
{
    public string Heading { get; set; } = "'Playfair Display', serif";
    public string Body { get; set; } = "'Open Sans', sans-serif";
    public string Accent { get; set; } = "'Merriweather', serif";
}

public class NavigationItem
{
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
}

public class SidebarConfig
{
    [YamlMember(Alias = "show_author")]
    public bool ShowAuthor { get; set; } = true;
    
    [YamlMember(Alias = "pinned_posts")]
    public List<string> PinnedPosts { get; set; } = new();
}

public class MarkdownConfig
{
    [YamlMember(Alias = "enable_lightbox")]
    public bool EnableLightbox { get; set; } = true;
    
    [YamlMember(Alias = "enable_video_embeds")]
    public bool EnableVideoEmbeds { get; set; } = true;
}
