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
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            UseCookies = true
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", _userAgent);
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        _httpClient.DefaultRequestHeaders.Add("DNT", "1");
        _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
        _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
    }

    public async Task<List<string>> SearchAsync(string query, int resultsCount)
    {
        // Демо-режим: генерируем случайные URL вместо реального поиска
        Console.WriteLine($"🔍 Демо-режим: Поиск \"{query}\" в Яндексе...");
        
        // Имитируем задержку поиска
        await Task.Delay(1500 + new Random().Next(1000));
        
        // Генерируем случайные URL
        var urls = FakeDataGenerator.GenerateSearchUrls(query, resultsCount);
        
        Console.WriteLine($"✅ Найдено {urls.Count} результатов (демо-данные)");
        
        return urls;
    }

    private async Task<List<string>> GetSearchResultsFromPageAsync(string query, int page)
    {
        var urls = new List<string>();

        try
        {
            var searchUrl = $"https://yandex.ru/search/?text={Uri.EscapeDataString(query)}&p={page}&lr=213";
            var html = await _httpClient.GetStringAsync(searchUrl);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Try multiple selectors aggressively
            var resultNodes = doc.DocumentNode.SelectNodes("//a[@href and not(ancestor::header) and not(ancestor::footer)]");
            
            if (resultNodes != null)
            {
                foreach (var node in resultNodes)
                {
                    var href = node.GetAttributeValue("href", "");
                    if (string.IsNullOrWhiteSpace(href))
                        continue;

                    // Extract actual URL
                    var actualUrl = ExtractActualUrl(href);
                    if (!string.IsNullOrWhiteSpace(actualUrl) && IsValidResultUrl(actualUrl))
                    {
                        urls.Add(actualUrl);
                        if (urls.Count >= 20) // Collect more than needed, will filter later
                            break;
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
            // Clean the URL
            href = href.Trim();

            // If it's already a direct URL
            if (href.StartsWith("http://") || href.StartsWith("https://"))
            {
                // Check if it's a Yandex redirect or internal link
                if (href.Contains("yandex."))
                {
                    return string.Empty; // Skip Yandex internal links
                }
                
                return href;
            }
            
            // If it's a protocol-relative URL
            if (href.StartsWith("//"))
            {
                href = "https:" + href;
                if (!href.Contains("yandex."))
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


