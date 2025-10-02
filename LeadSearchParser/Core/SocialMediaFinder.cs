using System.Text.RegularExpressions;
using LeadSearchParser.Models;

namespace LeadSearchParser.Core;

public class SocialMediaFinder
{
    private readonly SocialSettings _settings;

    public SocialMediaFinder(SocialSettings settings)
    {
        _settings = settings;
    }

    public Dictionary<string, string> ExtractSocialMedia(string html)
    {
        var result = new Dictionary<string, string>
        {
            { "VK", FindSocialLink(html, _settings.VK) },
            { "Telegram", FindSocialLink(html, _settings.Telegram) },
            { "WhatsApp", FindSocialLink(html, _settings.WhatsApp) },
            { "Instagram", FindSocialLink(html, _settings.Instagram) },
            { "Facebook", FindSocialLink(html, _settings.Facebook) },
            { "OK", FindSocialLink(html, _settings.OK) },
            { "YouTube", FindSocialLink(html, _settings.YouTube) }
        };

        return result;
    }

    private string FindSocialLink(string html, string pattern)
    {
        if (string.IsNullOrWhiteSpace(html) || string.IsNullOrWhiteSpace(pattern))
            return string.Empty;

        try
        {
            var patterns = pattern.Split('|');
            
            foreach (var p in patterns)
            {
                // Match full URLs
                var urlPattern = $@"https?://(?:www\.)?{Regex.Escape(p)}[^\s""'<>]*";
                var regex = new Regex(urlPattern, RegexOptions.IgnoreCase);
                var match = regex.Match(html);

                if (match.Success)
                {
                    return CleanUrl(match.Value);
                }

                // Match just domain with path
                var simplePattern = $@"{Regex.Escape(p)}[^\s""'<>]*";
                var simpleRegex = new Regex(simplePattern, RegexOptions.IgnoreCase);
                var simpleMatch = simpleRegex.Match(html);

                if (simpleMatch.Success)
                {
                    var url = simpleMatch.Value;
                    if (!url.StartsWith("http"))
                    {
                        url = "https://" + url;
                    }
                    return CleanUrl(url);
                }
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private string CleanUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        // Remove trailing punctuation
        url = url.TrimEnd('.', ',', ';', ')', ']', '}', '>', '"', '\'');
        
        // Remove query parameters for cleaner display (optional)
        var questionIndex = url.IndexOf('?');
        if (questionIndex > 0)
        {
            url = url.Substring(0, questionIndex);
        }

        return url;
    }
}


