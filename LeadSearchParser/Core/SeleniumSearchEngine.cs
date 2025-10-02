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
        InitializeDriver();
        if (_driver == null)
            return new List<string>();

        var urls = new HashSet<string>();

        try
        {
            // Открываем Яндекс
            var searchUrl = $"https://yandex.ru/search/?text={Uri.EscapeDataString(query)}";
            _driver.Navigate().GoToUrl(searchUrl);

            // Даем время на загрузку
            await Task.Delay(3000);

            // Собираем ссылки с нескольких страниц
            var pagesNeeded = (int)Math.Ceiling(resultsCount / 10.0);
            
            for (int page = 0; page < pagesNeeded && urls.Count < resultsCount; page++)
            {
                if (page > 0)
                {
                    // Переход на следующую страницу
                    try
                    {
                        var nextButton = _driver.FindElement(By.CssSelector("a.pager__item_kind_next, div.pager__item_kind_next"));
                        nextButton.Click();
                        await Task.Delay(3000);
                    }
                    catch
                    {
                        break; // Нет следующей страницы
                    }
                }

                // Извлекаем ссылки
                var pageUrls = ExtractUrls();
                foreach (var url in pageUrls)
                {
                    if (urls.Count >= resultsCount)
                        break;
                    urls.Add(url);
                }

                await Task.Delay(2000); // Задержка между страницами
            }

            return urls.Take(resultsCount).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при поиске: {ex.Message}");
            return urls.ToList();
        }
    }

    public async Task<List<string>> SearchGoogleAsync(string query, int resultsCount)
    {
        InitializeDriver();
        if (_driver == null)
            return new List<string>();

        var urls = new HashSet<string>();

        try
        {
            // Открываем Google
            var searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}&num={resultsCount}";
            _driver.Navigate().GoToUrl(searchUrl);

            // Даем время на загрузку
            await Task.Delay(3000);

            // Извлекаем ссылки
            var pageUrls = ExtractUrlsGoogle();
            urls.UnionWith(pageUrls);

            return urls.Take(resultsCount).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при поиске в Google: {ex.Message}");
            return urls.ToList();
        }
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

