using System.ComponentModel.DataAnnotations;

namespace QuillKit.Models;

/// <summary>
/// Validates that a string is either a valid URL (http, https, ftp) or a relative path.
/// </summary>
public class UrlOrRelativePathAttribute : ValidationAttribute
{
    // 🔍 Accepts fully-qualified URLs or relative paths
    public override bool IsValid(object? value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return true; // Null or empty is valid (field is optional)

        var urlString = value.ToString()!;

        // Check if it's a valid relative path FIRST (before Uri.TryCreate which treats /paths as file:// URIs)
        if (urlString.StartsWith("/") || urlString.StartsWith("./") || urlString.StartsWith("../"))
            return true;

        // Check if it's a valid fully-qualified URL
        if (Uri.TryCreate(urlString, UriKind.Absolute, out var absoluteUri))
        {
            var scheme = absoluteUri.Scheme;
            return scheme == Uri.UriSchemeHttp || scheme == Uri.UriSchemeHttps || scheme == Uri.UriSchemeFtp;
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field must be a valid URL (http, https, ftp) or a relative path (e.g., /media/image.jpg).";
    }
}
