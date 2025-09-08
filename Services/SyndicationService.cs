using QuillKit.Models;

namespace QuillKit.Services;

public class SyndicationService
{
    /// 📰 Generate RSS 2.0 XML feed content
    public string GenerateRssFeed(IEnumerable<Post> posts, SiteConfig siteConfig)
    {
        var baseUrl = !string.IsNullOrEmpty(siteConfig.BaseUrl) ? siteConfig.BaseUrl.TrimEnd('/') : "https://yoursite.com";
        var buildDate = FormatDateForRss(DateTime.UtcNow, siteConfig); // RFC 822 format in local time
        
        var rssItems = posts.Select(post => {
            var localPubDateRfc822 = FormatDateForRss(post.PubDate, siteConfig);
            
            return $@"
        <item>
            <title><![CDATA[{post.Title}]]></title>
            <link>{baseUrl}/post/{post.Slug}</link>
            <guid isPermaLink=""true"">{baseUrl}/post/{post.Slug}</guid>
            <description><![CDATA[{(!string.IsNullOrEmpty(post.Excerpt) ? post.Excerpt : post.Description)}]]></description>
            <pubDate>{localPubDateRfc822}</pubDate>
            <author>{siteConfig.Author.Name}</author>
            {string.Join("", post.Categories.Select(cat => $"<category><![CDATA[{cat}]]></category>"))}
        </item>";
        });

        var rssXml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<rss version=""2.0"" xmlns:atom=""http://www.w3.org/2005/Atom"">
    <channel>
        <title><![CDATA[{siteConfig.Title}]]></title>
        <link>{baseUrl}</link>
        <description><![CDATA[{siteConfig.Description}]]></description>
        <language>en-us</language>
        <lastBuildDate>{buildDate}</lastBuildDate>
        <atom:link href=""{baseUrl}/rss"" rel=""self"" type=""application/rss+xml"" />
        <generator>QuillKit</generator>
        <managingEditor>{siteConfig.Social.Email} ({siteConfig.Author.Name})</managingEditor>
        <webMaster>{siteConfig.Social.Email} ({siteConfig.Author.Name})</webMaster>
        <ttl>60</ttl>
        {string.Join("", rssItems)}
    </channel>
</rss>";

        return rssXml;
    }

    /// 📅 Format date for RSS feeds (RFC 822 format with timezone)
    private string FormatDateForRss(DateTime utcDateTime, SiteConfig siteConfig)
    {
        if (utcDateTime.Kind != DateTimeKind.Utc)
        {
            // If it's not UTC, format as-is
            return utcDateTime.ToString("R");
        }
        
        var timeZone = siteConfig.GetTimeZone();
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
        
        // Get timezone abbreviation (EST/EDT)
        var isDaylightSaving = timeZone.IsDaylightSavingTime(localTime);
        var tzAbbreviation = isDaylightSaving ? "EDT" : "EST";
        
        // Format: "Thu, 01 Jan 2015 00:00:00 EST"
        return $"{localTime:ddd, dd MMM yyyy HH:mm:ss} {tzAbbreviation}";
    }
}
