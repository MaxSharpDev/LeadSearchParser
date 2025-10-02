using System.Net;
using HtmlAgilityPack;
using LeadSearchParser.Models;

namespace LeadSearchParser.Core;

public class YandexSearchEngine
{
    private readonly HttpClient _httpClient;
    private readonly string _userAgent;

    public YandexSearchEngine(string userAgent)
    {
        _userAgent = userAgent;
        
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", _userAgent);
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9");
    }

    public async Task<List<string>> SearchAsync(string query, int resultsCount)
    {
        var allUrls = new HashSet<string>();
        var pagesNeeded = (int)Math.Ceiling(resultsCount / 10.0);

        for (int page = 0; page < pagesNeeded && allUrls.Count < resultsCount; page++)
        {
            var pageUrls = await GetSearchResultsFromPageAsync(query, page);
            foreach (var url in pageUrls)
            {
                if (allUrls.Count >= resultsCount)
                    break;
                allUrls.Add(url);
            }

            if (page < pagesNeeded - 1)
            {
                await Task.Delay(2000); // Delay between pages
            }
        }

        return allUrls.Take(resultsCount).ToList();
    }

    private async Task<List<string>> GetSearchResultsFromPageAsync(string query, int page)
    {
        var urls = new List<string>();

        try
        {
            var searchUrl = $"https://yandex.ru/search/?text={Uri.EscapeDataString(query)}&p={page}";
            var html = await _httpClient.GetStringAsync(searchUrl);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Yandex search result selectors (these may need adjustment)
            var resultNodes = doc.DocumentNode.SelectNodes("//li[contains(@class, 'serp-item')]//a[contains(@class, 'Link') and @href]");
            
            if (resultNodes != null)
            {
                foreach (var node in resultNodes)
                {
                    var href = node.GetAttributeValue("href", "");
                    if (string.IsNullOrWhiteSpace(href))
                        continue;

                    // Extract actual URL (Yandex may use redirects)
                    var actualUrl = ExtractActualUrl(href);
                    if (!string.IsNullOrWhiteSpace(actualUrl) && IsValidResultUrl(actualUrl))
                    {
                        urls.Add(actualUrl);
                    }
                }
            }

            // Alternative selector if the above doesn't work
            if (!urls.Any())
            {
                resultNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'organic')]//a[@href]");
                if (resultNodes != null)
                {
                    foreach (var node in resultNodes)
                    {
                        var href = node.GetAttributeValue("href", "");
                        if (string.IsNullOrWhiteSpace(href))
                            continue;

                        var actualUrl = ExtractActualUrl(href);
                        if (!string.IsNullOrWhiteSpace(actualUrl) && IsValidResultUrl(actualUrl))
                        {
                            urls.Add(actualUrl);
                            if (urls.Count >= 10)
                                break;
                        }
                    }
                }
            }

            // Try one more alternative selector
            if (!urls.Any())
            {
                resultNodes = doc.DocumentNode.SelectNodes("//h2/a[@href]");
                if (resultNodes != null)
                {
                    foreach (var node in resultNodes)
                    {
                        var href = node.GetAttributeValue("href", "");
                        var actualUrl = ExtractActualUrl(href);
                        if (!string.IsNullOrWhiteSpace(actualUrl) && IsValidResultUrl(actualUrl))
                        {
                            urls.Add(actualUrl);
                            if (urls.Count >= 10)
                                break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при поиске: {ex.Message}");
        }

        return urls.Distinct().ToList();
    }

    private string ExtractActualUrl(string href)
    {
        if (string.IsNullOrWhiteSpace(href))
            return string.Empty;

        try
        {
            // If it's already a direct URL
            if (href.StartsWith("http://") || href.StartsWith("https://"))
            {
                // Check if it's a Yandex redirect
                if (href.Contains("yandex.ru/clck") || href.Contains("yandex.ru/search"))
                {
                    // Try to extract the actual URL from query parameters
                    var uri = new Uri(href);
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    var url = query["url"] ?? query["text"];
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        return url;
                    }
                }
                else
                {
                    return href;
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return string.Empty;
    }

    private bool IsValidResultUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            var uri = new Uri(url);
            
            // Exclude Yandex's own domains and common non-result URLs
            var host = uri.Host.ToLower();
            if (host.Contains("yandex.") || 
                host.Contains("google.") || 
                host.Contains("wikipedia.") ||
                host == "localhost")
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}


