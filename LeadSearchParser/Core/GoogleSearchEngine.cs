using System.Net;
using HtmlAgilityPack;

namespace LeadSearchParser.Core;

/// <summary>
/// Поисковый движок Google (HTTP запросы)
/// Менее строгие ограничения чем у Яндекса
/// </summary>
public class GoogleSearchEngine
{
    private readonly HttpClient _httpClient;
    private readonly string _userAgent;

    public GoogleSearchEngine(string userAgent)
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
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        _httpClient.DefaultRequestHeaders.Add("DNT", "1");
        _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
        _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
    }

    public async Task<List<string>> SearchAsync(string query, int resultsCount)
    {
        // Демо-режим: генерируем случайные URL вместо реального поиска
        Console.WriteLine($"🔍 Демо-режим: Поиск \"{query}\" в Google...");
        
        // Имитируем задержку поиска
        await Task.Delay(2000 + new Random().Next(1500));
        
        // Генерируем случайные URL
        var urls = FakeDataGenerator.GenerateSearchUrls(query, resultsCount);
        
        Console.WriteLine($"✅ Найдено {urls.Count} результатов (демо-данные)");
        
        return urls;
    }

    private async Task<List<string>> GetSearchResultsFromPageAsync(string query, int start)
    {
        var urls = new List<string>();

        try
        {
            var searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}&start={start}&hl=ru";
            var html = await _httpClient.GetStringAsync(searchUrl);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Извлекаем ссылки из результатов поиска
            // Google использует разные селекторы в зависимости от версии
            var selectors = new[]
            {
                "//div[@class='yuRUbf']//a[@href]",
                "//div[contains(@class, 'g')]//a[@href]",
                "//a[@href and contains(@href, 'http')]"
            };

            foreach (var selector in selectors)
            {
                var resultNodes = doc.DocumentNode.SelectNodes(selector);
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

                if (urls.Any())
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при поиске в Google: {ex.Message}");
        }

        return urls.Distinct().ToList();
    }

    private string ExtractActualUrl(string href)
    {
        if (string.IsNullOrWhiteSpace(href))
            return string.Empty;

        try
        {
            href = href.Trim();

            // Прямая ссылка
            if (href.StartsWith("http://") || href.StartsWith("https://"))
            {
                // Пропускаем Google редиректы
                if (href.Contains("google.com/url"))
                {
                    var uri = new Uri(href);
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    var url = query["url"] ?? query["q"];
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        return url;
                    }
                }
                
                if (!href.Contains("google."))
                {
                    return href;
                }
            }

            // Protocol-relative URL
            if (href.StartsWith("//"))
            {
                href = "https:" + href;
                if (!href.Contains("google."))
                {
                    return href;
                }
            }
        }
        catch
        {
            // Игнорируем ошибки
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
            var host = uri.Host.ToLower();

            // Исключаем служебные домены
            var invalidDomains = new[] 
            { 
                "google.", 
                "yandex.",
                "youtube.",
                "facebook.",
                "instagram.",
                "wikipedia.",
                "localhost"
            };

            if (invalidDomains.Any(d => host.Contains(d)))
                return false;

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

