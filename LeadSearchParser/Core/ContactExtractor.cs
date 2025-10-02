using System.Text.RegularExpressions;
using HtmlAgilityPack;
using LeadSearchParser.Models;

namespace LeadSearchParser.Core;

public class ContactExtractor
{
    private readonly ExtractorSettings _settings;
    private readonly Regex _emailRegex;
    private readonly List<Regex> _phoneRegexes;

    public ContactExtractor(ExtractorSettings settings)
    {
        _settings = settings;
        _emailRegex = new Regex(_settings.EmailPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        _phoneRegexes = _settings.PhonePatterns
            .Select(p => new Regex(p, RegexOptions.Compiled))
            .ToList();
    }

    public List<string> ExtractEmails(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return new List<string>();

        var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Extract from text content
        var matches = _emailRegex.Matches(html);
        foreach (Match match in matches)
        {
            var email = match.Value.ToLower();
            if (Utils.Validator.IsValidEmail(email))
            {
                emails.Add(email);
            }
        }

        // Extract from mailto links
        var mailtoPattern = new Regex(@"mailto:([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})", 
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var mailtoMatches = mailtoPattern.Matches(html);
        foreach (Match match in mailtoMatches)
        {
            if (match.Groups.Count > 1)
            {
                var email = match.Groups[1].Value.ToLower();
                if (Utils.Validator.IsValidEmail(email))
                {
                    emails.Add(email);
                }
            }
        }

        return emails.ToList();
    }

    public List<string> ExtractPhones(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return new List<string>();

        var phones = new HashSet<string>();

        foreach (var regex in _phoneRegexes)
        {
            var matches = regex.Matches(html);
            foreach (Match match in matches)
            {
                var phone = match.Value;
                if (Utils.Validator.IsValidPhone(phone))
                {
                    phones.Add(Utils.Validator.NormalizePhone(phone));
                }
            }
        }

        return phones.ToList();
    }

    public string ExtractTitle(HtmlDocument doc)
    {
        try
        {
            var title = "";

            // Try title tag
            var titleNode = doc.DocumentNode.SelectSingleNode("//title");
            if (titleNode != null && !string.IsNullOrWhiteSpace(titleNode.InnerText))
            {
                title = CleanText(titleNode.InnerText);
                if (IsValidTitle(title))
                    return title;
            }

            // Try meta og:title
            var ogTitleNode = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']");
            if (ogTitleNode != null)
            {
                var content = ogTitleNode.GetAttributeValue("content", "");
                if (!string.IsNullOrWhiteSpace(content))
                {
                    title = CleanText(content);
                    if (IsValidTitle(title))
                        return title;
                }
            }

            // Try h1
            var h1Node = doc.DocumentNode.SelectSingleNode("//h1");
            if (h1Node != null && !string.IsNullOrWhiteSpace(h1Node.InnerText))
            {
                title = CleanText(h1Node.InnerText);
                if (IsValidTitle(title))
                    return title;
            }

            // Try meta description as fallback
            var descNode = doc.DocumentNode.SelectSingleNode("//meta[@name='description']");
            if (descNode != null)
            {
                var content = descNode.GetAttributeValue("content", "");
                if (!string.IsNullOrWhiteSpace(content))
                {
                    title = CleanText(content);
                    if (title.Length > 10 && title.Length < 100)
                        return title;
                }
            }

            return string.IsNullOrWhiteSpace(title) ? "Без названия" : title;
        }
        catch
        {
            return "Без названия";
        }
    }

    private bool IsValidTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return false;

        // Фильтруем подозрительные заголовки
        var lowerTitle = title.ToLower();
        var invalidTitles = new[] 
        { 
            "вы не робот", 
            "you are not a robot",
            "404", 
            "403", 
            "error", 
            "ошибка",
            "access denied",
            "доступ запрещен",
            "loading",
            "загрузка",
            "captcha",
            "капча"
        };

        if (invalidTitles.Any(invalid => lowerTitle.Contains(invalid)))
            return false;

        // Минимальная длина
        if (title.Length < 3)
            return false;

        return true;
    }

    private string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = System.Net.WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }
}


