using YamlDotNet.Serialization;

namespace QuillKit.Models;

public class SiteConfig
{
    // Site Settings
    public string Title { get; set; } = "QuillKit";
    public string Subtitle { get; set; } = "Your thoughts, published beautifully";
    public string Description { get; set; } = "";
    public string Keywords { get; set; } = "";
    [YamlMember(Alias = "baseurl")]
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
    
    [YamlMember(Alias = "timezone")]
    public string Timezone { get; set; } = "America/New_York"; // Eastern Time (handles EST/EDT automatically)
    
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

    // Timezone utilities
    public TimeZoneInfo GetTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(Timezone);
        }
        catch (TimeZoneNotFoundException)
        {
            // Fallback to Eastern Time if timezone is invalid
            return TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        }
    }

    public DateTime ConvertToLocalTime(DateTime utcDateTime)
    {
        if (utcDateTime.Kind != DateTimeKind.Utc)
        {
            // If it's not UTC, assume it's already in the desired timezone
            return utcDateTime;
        }
        
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, GetTimeZone());
    }

    public string FormatDate(DateTime dateTime, string? customFormat = null)
    {
        var localTime = ConvertToLocalTime(dateTime);
        return localTime.ToString(customFormat ?? DateFormat);
    }
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
    [YamlMember(Alias = "github")]
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
