using System.Net;
using HtmlAgilityPack;
using LeadSearchParser.Models;

namespace LeadSearchParser.Core;

public class WebParser
{
    private readonly ParserSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly HashSet<string> _visitedUrls = new();

    public WebParser(ParserSettings settings)
    {
        _settings = settings;
        
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(_settings.Timeout)
        };
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", _settings.UserAgent);
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
    }

    public async Task<string> GetPageContentAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return string.Empty;
        }
    }

    public async Task<HtmlDocument?> LoadPageAsync(string url)
    {
        try
        {
            var html = await GetPageContentAsync(url);
            if (string.IsNullOrWhiteSpace(html))
                return null;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<string>> GetInternalLinksAsync(string baseUrl, int depth, ExtractorSettings extractorSettings)
    {
        _visitedUrls.Clear();
        var allUrls = new List<string> { baseUrl };
        
        try
        {
            var uri = new Uri(baseUrl);
            var baseDomain = uri.Host;

            // Get main page links
            var mainPageLinks = await GetLinksFromPageAsync(baseUrl, baseDomain);
            allUrls.AddRange(mainPageLinks);

            // Look for contact pages specifically
            var contactUrls = FindContactPages(mainPageLinks, extractorSettings.ContactPages);
            
            // If depth > 1, crawl additional pages
            if (depth > 1)
            {
                var urlsToVisit = contactUrls.Any() ? contactUrls : mainPageLinks.Take(Math.Min(10, mainPageLinks.Count)).ToList();
                
                foreach (var url in urlsToVisit)
                {
                    if (_visitedUrls.Count >= depth * 10)
                        break;

                    var subLinks = await GetLinksFromPageAsync(url, baseDomain);
                    allUrls.AddRange(subLinks);
                    
                    await Task.Delay(_settings.Delay * 1000);
                }
            }

            return allUrls.Distinct().ToList();
        }
        catch
        {
            return allUrls;
        }
    }

    private async Task<List<string>> GetLinksFromPageAsync(string url, string baseDomain)
    {
        var links = new List<string>();

        if (_visitedUrls.Contains(url))
            return links;

        _visitedUrls.Add(url);

        try
        {
            var doc = await LoadPageAsync(url);
            if (doc == null)
                return links;

            var linkNodes = doc.DocumentNode.SelectNodes("//a[@href]");
            if (linkNodes == null)
                return links;

            foreach (var linkNode in linkNodes)
            {
                var href = linkNode.GetAttributeValue("href", "");
                if (string.IsNullOrWhiteSpace(href))
                    continue;

                var absoluteUrl = GetAbsoluteUrl(url, href);
                if (string.IsNullOrWhiteSpace(absoluteUrl))
                    continue;

                // Only include links from the same domain
                if (Uri.TryCreate(absoluteUrl, UriKind.Absolute, out var uri))
                {
                    if (uri.Host == baseDomain && !_visitedUrls.Contains(absoluteUrl))
                    {
                        links.Add(absoluteUrl);
                    }
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return links;
    }

    private List<string> FindContactPages(List<string> urls, List<string> contactKeywords)
    {
        var contactPages = new List<string>();

        foreach (var url in urls)
        {
            var lowerUrl = url.ToLower();
            if (contactKeywords.Any(keyword => lowerUrl.Contains(keyword.ToLower())))
            {
                contactPages.Add(url);
            }
        }

        return contactPages;
    }

    private string GetAbsoluteUrl(string baseUrl, string relativeUrl)
    {
        try
        {
            if (relativeUrl.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
                relativeUrl.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                relativeUrl.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
                relativeUrl.StartsWith("#"))
            {
                return string.Empty;
            }

            if (Uri.TryCreate(relativeUrl, UriKind.Absolute, out _))
            {
                return relativeUrl;
            }

            var baseUri = new Uri(baseUrl);
            var absoluteUri = new Uri(baseUri, relativeUrl);
            return absoluteUri.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}


