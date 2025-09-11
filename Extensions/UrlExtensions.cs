using QuillKit.Services;

namespace QuillKit.Extensions;

/// <summary>
/// Extension methods for URL handling with base URL support
/// </summary>
public static class UrlExtensions
{
    /// <summary>
    /// 🔗 Converts a relative URL to an absolute URL using the site's base URL configuration
    /// </summary>
    public static string ToAbsoluteUrl(this string url, SiteConfigService siteConfigService)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        // If it's already an absolute URL (starts with http/https), return as-is
        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return url;

        var baseUrl = siteConfigService.Config.BaseUrl?.Trim() ?? "";
        
        // If no base URL configured, return the original relative URL
        if (string.IsNullOrWhiteSpace(baseUrl))
            return url;

        // If baseUrl is a full URL (contains http), extract just the path part for local development
        if (baseUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uri = new Uri(baseUrl);
                baseUrl = uri.AbsolutePath.TrimEnd('/');
            }
            catch
            {
                // If URI parsing fails, just use as-is
                baseUrl = baseUrl.TrimEnd('/');
            }
        }
        else
        {
            baseUrl = baseUrl.TrimEnd('/');
        }

        // Ensure the URL starts with a slash
        var cleanUrl = url.StartsWith('/') ? url : '/' + url;
        
        return string.IsNullOrWhiteSpace(baseUrl) ? cleanUrl : $"{baseUrl}{cleanUrl}";
    }

    /// <summary>
    /// 🖼️ Converts an image URL to an absolute URL, handling both relative and absolute paths
    /// </summary>
    public static string ToAbsoluteImageUrl(this string imageUrl, SiteConfigService siteConfigService)
    {
        return imageUrl.ToAbsoluteUrl(siteConfigService);
    }

    /// <summary>
    /// 🔗 Converts a relative URL to an absolute URL using a fallback request context when base URL is not configured
    /// </summary>
    public static string ToAbsoluteUrl(this string url, SiteConfigService siteConfigService, Microsoft.AspNetCore.Http.HttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        // If it's already an absolute URL (starts with http/https), return as-is
        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return url;

        var baseUrl = siteConfigService.Config.BaseUrl?.TrimEnd('/');
        
        // If no base URL configured, fall back to request URL
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = $"{request.Scheme}://{request.Host}";
        }

        // Ensure the URL starts with a slash
        var cleanUrl = url.StartsWith('/') ? url : '/' + url;
        
        return $"{baseUrl}{cleanUrl}";
    }
}
