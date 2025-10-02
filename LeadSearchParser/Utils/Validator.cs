using System.Text.RegularExpressions;

namespace LeadSearchParser.Utils;

public static class Validator
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex UrlRegex = new(
        @"^https?://[^\s/$.?#].[^\s]*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        // Additional checks for common false positives
        var lowerEmail = email.ToLower();
        if (lowerEmail.EndsWith(".png") || lowerEmail.EndsWith(".jpg") || 
            lowerEmail.EndsWith(".gif") || lowerEmail.EndsWith(".jpeg") ||
            lowerEmail.EndsWith(".svg") || lowerEmail.EndsWith(".webp"))
            return false;

        return EmailRegex.IsMatch(email);
    }

    public static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return UrlRegex.IsMatch(url) && Uri.TryCreate(url, UriKind.Absolute, out _);
    }

    public static bool IsValidPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        // Remove all non-digit characters for validation
        var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());
        
        // Russian phone numbers should have 10 or 11 digits
        return digitsOnly.Length >= 10 && digitsOnly.Length <= 11;
    }

    public static string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        url = url.Trim();

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        return url;
    }

    public static string NormalizePhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        // Keep formatting for readability
        return phone.Trim();
    }
}


