using QuillKit.Services;

namespace QuillKit.Extensions;

/// <summary>
/// Extension methods for date formatting with timezone support
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// 📅 Format a DateTime using the site's timezone and date format settings
    /// </summary>
    public static string ToSiteLocalTime(this DateTime dateTime, SiteConfigService siteConfigService, string? customFormat = null)
    {
        return siteConfigService.Config.FormatDate(dateTime, customFormat);
    }

    /// <summary>
    /// 📅 Convert UTC DateTime to site's local timezone
    /// </summary>
    public static DateTime ToSiteTimeZone(this DateTime utcDateTime, SiteConfigService siteConfigService)
    {
        return siteConfigService.Config.ConvertToLocalTime(utcDateTime);
    }

    /// <summary>
    /// 📅 Format a DateTime for RSS feeds (RFC 822 format) in site's timezone
    /// </summary>
    public static string ToRssDate(this DateTime dateTime, SiteConfigService siteConfigService)
    {
        var localTime = siteConfigService.Config.ConvertToLocalTime(dateTime);
        return localTime.ToString("R");
    }
}
