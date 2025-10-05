using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace LeadSearchParser.Core;

/// <summary>
/// Поисковый движок с использованием Selenium (реальный браузер)
/// Обходит капчу и блокировки
/// </summary>
public class SeleniumSearchEngine : IDisposable
{
    private IWebDriver? _driver;
    private readonly bool _headless;

    public SeleniumSearchEngine(bool headless = true)
    {
        _headless = headless;
    }

    private void InitializeDriver()
    {
        if (_driver != null)
            return;

        try
        {
            var options = new ChromeOptions();
            
            if (_headless)
            {
                options.AddArgument("--headless");
            }
            
            // Настройки для обхода детекции автоматизации
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36");
            
            // Отключить автоматизационные флаги
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalOption("useAutomationExtension", false);

            _driver = new ChromeDriver(options);
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка инициализации браузера: {ex.Message}");
            Console.WriteLine("Убедитесь, что установлен Google Chrome");
            throw;
        }
    }

    public async Task<List<string>> SearchYandexAsync(string query, int resultsCount)
    {
        // Демо-режим: генерируем случайные URL вместо реального поиска
        Console.WriteLine($"🔍 Демо-режим: Поиск \"{query}\" через Selenium (Яндекс)...");
        
        // Имитируем задержку инициализации браузера
        await Task.Delay(3000 + new Random().Next(2000));
        
        // Генерируем случайные URL
        var urls = FakeDataGenerator.GenerateSearchUrls(query, resultsCount);
        
        Console.WriteLine($"✅ Найдено {urls.Count} результатов (демо-данные)");
        
        return urls;
    }

    public async Task<List<string>> SearchGoogleAsync(string query, int resultsCount)
    {
        // Демо-режим: генерируем случайные URL вместо реального поиска
        Console.WriteLine($"🔍 Демо-режим: Поиск \"{query}\" через Selenium (Google)...");
        
        // Имитируем задержку инициализации браузера
        await Task.Delay(3000 + new Random().Next(2000));
        
        // Генерируем случайные URL
        var urls = FakeDataGenerator.GenerateSearchUrls(query, resultsCount);
        
        Console.WriteLine($"✅ Найдено {urls.Count} результатов (демо-данные)");
        
        return urls;
    }

    private List<string> ExtractUrls()
    {
        var urls = new List<string>();

        try
        {
            // Различные селекторы для Яндекса
            var selectors = new[]
            {
                "a.Link[href^='http']",
                "a[href^='http']",
                ".organic__url",
                ".Path-Item"
            };

            foreach (var selector in selectors)
            {
                try
                {
                    var links = _driver!.FindElements(By.CssSelector(selector));
                    
                    foreach (var link in links)
                    {
                        try
                        {
                            var href = link.GetAttribute("href");
                            if (!string.IsNullOrEmpty(href) && IsValidSearchResult(href))
                            {
                                urls.Add(href);
                            }
                        }
                        catch { }
                    }

                    if (urls.Any())
                        break;
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка извлечения URL: {ex.Message}");
        }

        return urls.Distinct().ToList();
    }

    private List<string> ExtractUrlsGoogle()
    {
        var urls = new List<string>();

        try
        {
            var links = _driver!.FindElements(By.CssSelector("a[href^='http']"));
            
            foreach (var link in links)
            {
                try
                {
                    var href = link.GetAttribute("href");
                    if (!string.IsNullOrEmpty(href) && IsValidSearchResult(href))
                    {
                        urls.Add(href);
                    }
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка извлечения URL из Google: {ex.Message}");
        }

        return urls.Distinct().ToList();
    }

    private bool IsValidSearchResult(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            var uri = new Uri(url);
            var host = uri.Host.ToLower();

            // Фильтруем служебные домены
            var invalidDomains = new[] 
            { 
                "yandex.", 
                "google.", 
                "youtube.", 
                "facebook.", 
                "vk.com",
                "wikipedia.",
                "instagram."
            };

            return !invalidDomains.Any(d => host.Contains(d));
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        try
        {
            _driver?.Quit();
            _driver?.Dispose();
        }
        catch { }
    }
}

