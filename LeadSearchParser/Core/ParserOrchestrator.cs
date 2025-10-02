using System.Collections.Concurrent;
using System.Diagnostics;
using LeadSearchParser.Models;
using LeadSearchParser.Utils;

namespace LeadSearchParser.Core;

public class ParserOrchestrator
{
    private readonly ParserConfig _config;
    private readonly Logger _logger;
    private readonly YandexSearchEngine _searchEngine;
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
        _searchEngine = new YandexSearchEngine(_config.Parser.UserAgent);
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

    public async Task<List<SiteData>> RunAsync(string query, int resultsCount, int depth)
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.Info($"Запрос: \"{query}\"");
        _logger.Info($"Настройки: результатов={resultsCount}, глубина={depth}, задержка={_config.Parser.Delay}сек");
        Console.WriteLine();

        // Step 1: Search Yandex
        _logger.Info("Поиск в Яндексе...");
        var urls = await _searchEngine.SearchAsync(query, resultsCount);
        _logger.Success($"Найдено URL: {urls.Count}");
        Console.WriteLine();

        if (!urls.Any())
        {
            _logger.Warning("Не найдено результатов поиска");
            _logger.Info("РЕШЕНИЕ: Используйте файл urls.txt с прямыми ссылками на сайты");
            _logger.Info("Пример: LeadSearchParser.exe --urls urls.txt");
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
            var webParser = new WebParser(_config.Parser);
            var contactExtractor = new ContactExtractor(_config.Extractor);
            var socialFinder = new SocialMediaFinder(_config.Social);

            // Load main page
            var mainDoc = await webParser.LoadPageAsync(url);
            if (mainDoc == null)
            {
                siteData.IsSuccess = false;
                siteData.ErrorMessage = "Не удалось загрузить страницу";
                return siteData;
            }

            // Extract title
            siteData.Title = contactExtractor.ExtractTitle(mainDoc);
            
            // Если название подозрительное, используем домен
            if (siteData.Title == "Без названия" || siteData.Title.Length < 3)
            {
                siteData.Title = GetDomain(url);
            }

            // Get all pages to parse
            var urlsToParse = await webParser.GetInternalLinksAsync(url, depth, _config.Extractor);

            // Parse all pages and collect data
            var allEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var allPhones = new HashSet<string>();
            var allHtml = new List<string>();

            foreach (var pageUrl in urlsToParse.Take(depth * 10)) // Limit total pages
            {
                try
                {
                    var html = await webParser.GetPageContentAsync(pageUrl);
                    if (!string.IsNullOrWhiteSpace(html))
                    {
                        allHtml.Add(html);

                        var emails = contactExtractor.ExtractEmails(html);
                        var phones = contactExtractor.ExtractPhones(html);

                        foreach (var email in emails)
                            allEmails.Add(email);

                        foreach (var phone in phones)
                            allPhones.Add(phone);
                    }

                    await Task.Delay(500); // Small delay between pages
                }
                catch
                {
                    // Continue with other pages
                }
            }

            siteData.Emails = allEmails.ToList();
            siteData.Phones = allPhones.ToList();

            // Extract social media from all collected HTML
            var combinedHtml = string.Join("\n", allHtml);
            var socialMedia = socialFinder.ExtractSocialMedia(combinedHtml);

            siteData.VK = socialMedia.GetValueOrDefault("VK", string.Empty);
            siteData.Telegram = socialMedia.GetValueOrDefault("Telegram", string.Empty);

            siteData.IsSuccess = true;
            webParser.Dispose();
        }
        catch (Exception ex)
        {
            siteData.IsSuccess = false;
            siteData.ErrorMessage = ex.Message.Length > 100 ? ex.Message.Substring(0, 100) : ex.Message;
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


