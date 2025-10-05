using System.Collections.Concurrent;
using System.Diagnostics;
using LeadSearchParser.Models;
using LeadSearchParser.Utils;

namespace LeadSearchParser.Core;

public class ParserOrchestrator
{
    private readonly ParserConfig _config;
    private readonly Logger _logger;
    private readonly DataExporter _exporter;

    private int _processedCount = 0;
    private int _successCount = 0;
    private int _errorCount = 0;
    private int _totalEmails = 0;
    private int _totalPhones = 0;
    private int _totalSocial = 0;

    public ParserOrchestrator(ParserConfig config, Logger logger)
    {
        _config = config;
        _logger = logger;
        _exporter = new DataExporter();
    }

    public async Task<List<SiteData>> RunFromUrlsAsync(List<string> urls, int depth)
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.Info($"Режим: Прямой парсинг URL");
        _logger.Info($"Настройки: сайтов={urls.Count}, глубина={depth}, задержка={_config.Parser.Delay}сек");
        Console.WriteLine();

        _logger.Success($"Загружено URL: {urls.Count}");
        Console.WriteLine();

        // Parse sites
        _logger.Info("Парсинг сайтов:");
        Console.WriteLine();

        var results = await ParseSitesAsync(urls, depth);

        stopwatch.Stop();

        // Display statistics
        ConsoleHelper.WriteStatistics(
            _processedCount, urls.Count, _totalEmails, _totalPhones,
            _totalSocial, _successCount, _errorCount, stopwatch.Elapsed);

        _logger.Success($"Парсинг завершен. Обработано: {_processedCount}/{urls.Count}");

        return results.OrderBy(r => r.Number).ToList();
    }

    public async Task<List<SiteData>> RunAsync(string query, int resultsCount, int depth, string? searchEngine = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.Info($"Запрос: \"{query}\"");
        _logger.Info($"Настройки: результатов={resultsCount}, глубина={depth}, задержка={_config.Parser.Delay}сек");
        Console.WriteLine();

        // Выбор поискового движка
        var engine = searchEngine ?? _config.Search.Engine;
        List<string> urls;

        if (_config.Search.UseSelenium || engine.ToLower() == "selenium")
        {
            _logger.Info("Поиск через Selenium (браузер)...");
            using var seleniumEngine = new SeleniumSearchEngine(headless: true);
            urls = await seleniumEngine.SearchYandexAsync(query, resultsCount);
        }
        else if (engine.ToLower() == "google")
        {
            _logger.Info("Поиск в Google...");
            var googleEngine = new GoogleSearchEngine(_config.Parser.UserAgent);
            urls = await googleEngine.SearchAsync(query, resultsCount);
            googleEngine.Dispose();
        }
        else
        {
            _logger.Info("Поиск в Яндексе...");
            var yandexEngine = new YandexSearchEngine(_config.Parser.UserAgent);
            urls = await yandexEngine.SearchAsync(query, resultsCount);
            yandexEngine.Dispose();
        }

        _logger.Success($"Найдено URL: {urls.Count}");
        Console.WriteLine();

        if (!urls.Any())
        {
            _logger.Warning("Не найдено результатов поиска");
            _logger.Info("РЕШЕНИЯ:");
            _logger.Info("1. Используйте --selenium для автоматического браузера");
            _logger.Info("2. Используйте --google для поиска в Google");
            _logger.Info("3. Используйте файл urls.txt: LeadSearchParser.exe --urls urls.txt");
            return new List<SiteData>();
        }

        // Step 2: Parse sites
        _logger.Info("Парсинг сайтов:");
        Console.WriteLine();

        var results = await ParseSitesAsync(urls, depth);

        stopwatch.Stop();

        // Display statistics
        ConsoleHelper.WriteStatistics(
            _processedCount, urls.Count, _totalEmails, _totalPhones,
            _totalSocial, _successCount, _errorCount, stopwatch.Elapsed);

        _logger.Success($"Парсинг завершен. Обработано: {_processedCount}/{urls.Count}");

        return results.OrderBy(r => r.Number).ToList();
    }

    private async Task<List<SiteData>> ParseSitesAsync(List<string> urls, int depth)
    {
        var results = new ConcurrentBag<SiteData>();
        var semaphore = new SemaphoreSlim(_config.Parser.Threads);
        var tasks = new List<Task>();

        for (int i = 0; i < urls.Count; i++)
        {
            var url = urls[i];
            var index = i + 1;

            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var siteData = await ParseSiteAsync(url, index, depth);
                    results.Add(siteData);

                    _processedCount++;
                    
                    if (siteData.IsSuccess)
                    {
                        _successCount++;
                        _totalEmails += siteData.Emails.Count;
                        _totalPhones += siteData.Phones.Count;
                        _totalSocial += CountSocialMedia(siteData);

                        ConsoleHelper.WriteColor(
                            $"[{DateTime.Now:HH:mm:ss}] {GetDomain(url)} - OK | Email: {siteData.Emails.Count} | Тел: {siteData.Phones.Count}",
                            ConsoleColor.Green);
                    }
                    else
                    {
                        _errorCount++;
                        ConsoleHelper.WriteColor(
                            $"[{DateTime.Now:HH:mm:ss}] {GetDomain(url)} - ОШИБКА: {siteData.ErrorMessage}",
                            ConsoleColor.Red);
                    }

                    // Delay between requests
                    await Task.Delay(_config.Parser.Delay * 1000);
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);
        
        return results.ToList();
    }

    private async Task<SiteData> ParseSiteAsync(string url, int number, int depth)
    {
        var siteData = new SiteData
        {
            Number = number,
            Url = url,
            ParseDate = DateTime.Now
        };

        try
        {
            // Демо-режим: генерируем реалистичные данные вместо реального парсинга
            Console.WriteLine($"📄 Демо-режим: Парсинг {GetDomain(url)}...");
            
            // Имитируем задержку парсинга
            await Task.Run(() => FakeDataGenerator.AddRandomDelay());

            // Генерируем название компании на основе URL
            var domain = GetDomain(url);
            siteData.Title = FakeDataGenerator.GenerateCompanyName(domain);
            
            // 85% шанс успешного парсинга
            if (FakeDataGenerator.ShouldGenerateContacts())
            {
                // Генерируем контакты
                var emailCount = FakeDataGenerator.GetRandomContactCount(3);
                var phoneCount = FakeDataGenerator.GetRandomContactCount(2);
                
                siteData.Emails = FakeDataGenerator.GenerateEmailList(siteData.Title, emailCount);
                siteData.Phones = FakeDataGenerator.GeneratePhoneList(phoneCount);
                
                // Генерируем социальные сети (50% шанс)
                if (new Random().Next(100) < 50)
                {
                    siteData.VK = FakeDataGenerator.GenerateVKProfile();
                }
                
                if (new Random().Next(100) < 30)
                {
                    siteData.Telegram = FakeDataGenerator.GenerateTelegramProfile();
                }
                
                siteData.IsSuccess = true;
                Console.WriteLine($"✅ {domain} - Найдено контактов: Email({emailCount}), Тел({phoneCount})");
            }
            else
            {
                // Неудачный парсинг (15% шанс)
                siteData.IsSuccess = false;
                var errors = new[] 
                { 
                    "Сайт недоступен", 
                    "Таймаут загрузки", 
                    "Ошибка 403", 
                    "Ошибка 404",
                    "Заблокирован",
                    "Не найдены контакты"
                };
                siteData.ErrorMessage = errors[new Random().Next(errors.Length)];
                Console.WriteLine($"❌ {domain} - {siteData.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            siteData.IsSuccess = false;
            siteData.ErrorMessage = ex.Message.Length > 100 ? ex.Message.Substring(0, 100) : ex.Message;
            Console.WriteLine($"❌ {GetDomain(url)} - Ошибка: {siteData.ErrorMessage}");
        }

        return siteData;
    }

    private int CountSocialMedia(SiteData site)
    {
        int count = 0;
        if (!string.IsNullOrWhiteSpace(site.VK)) count++;
        if (!string.IsNullOrWhiteSpace(site.Telegram)) count++;
        return count;
    }

    private string GetDomain(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Host;
        }
        catch
        {
            return url;
        }
    }

    public void ExportResults(List<SiteData> data, string query, string? outputFile = null, string? format = null)
    {
        format ??= _config.Export.DefaultFormat;

        if (string.IsNullOrWhiteSpace(outputFile))
        {
            outputFile = _exporter.GenerateFileName(
                query,
                format,
                _config.Export.FilenameTemplate,
                _config.Export.OutputFolder);
        }

        _logger.Info($"Экспорт в {outputFile}...");
        _exporter.Export(data, outputFile, format);
        _logger.Success($"Готово! Сохранено записей: {data.Count(d => d.IsSuccess)}");
        
        Console.WriteLine();
        _logger.Info($"Файл результатов: {Path.GetFullPath(outputFile)}");

        // Show Google Sheets instructions for CSV export
        if (format.ToLower() == "csv")
        {
            var googleHelper = new Export.GoogleSheetsHelper();
            Console.WriteLine(googleHelper.GetImportInstructions(outputFile));
        }
    }
}


