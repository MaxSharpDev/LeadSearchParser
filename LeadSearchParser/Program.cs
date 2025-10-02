using LeadSearchParser.Core;
using LeadSearchParser.Models;
using LeadSearchParser.Utils;

namespace LeadSearchParser;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            // Parse command line arguments
            var options = ParseArguments(args);

            // Load configuration
            var config = ConfigManager.LoadConfig(options.ConfigFile);

            // Initialize logger
            var logFile = Path.Combine(Directory.GetCurrentDirectory(), config.Logging.File);
            var logger = new Logger(logFile, config.Logging.Enabled);

            logger.Info($"LeadSearchParser v1.0 запущен");
            logger.Info($"Рабочая директория: {Directory.GetCurrentDirectory()}");

            if (!string.IsNullOrEmpty(options.ConfigFile) && File.Exists(options.ConfigFile))
            {
                logger.Info($"Конфиг загружен: {options.ConfigFile}");
            }

            Console.WriteLine();
            ConsoleHelper.WriteHeader();

            // Обработка специальных команд
            var cleanupManager = new FileCleanupManager(config.Export.OutputFolder, config.Cleanup.KeepDays, logger);

            if (options.ShowStorage)
            {
                cleanupManager.ShowStorageInfo();
                return 0;
            }

            if (options.CleanupAll)
            {
                logger.Warning("Удаление ВСЕХ файлов из папки результатов...");
                cleanupManager.CleanupAllFiles();
                return 0;
            }

            if (options.Cleanup)
            {
                logger.Info("Очистка старых файлов...");
                cleanupManager.CleanupOldFiles();
                return 0;
            }

            // Автоматическая очистка при запуске (если включена)
            if (config.Cleanup.Enabled && config.Cleanup.AutoCleanup)
            {
                if (cleanupManager.ShouldAutoCleanup())
                {
                    logger.Info($"Автоматическая очистка файлов старше {config.Cleanup.KeepDays} дн...");
                    cleanupManager.CleanupOldFiles();
                    Console.WriteLine();
                }
            }

            // Handle interactive mode or query from arguments
            string query;
            int resultsCount;
            int depth;

            if (string.IsNullOrWhiteSpace(options.Query) && string.IsNullOrWhiteSpace(options.QueriesFile))
            {
                // Interactive mode
                query = ConsoleHelper.ReadInput("Введите поисковый запрос");
                if (string.IsNullOrWhiteSpace(query))
                {
                    ConsoleHelper.WriteColor("Ошибка: необходимо указать поисковый запрос", ConsoleColor.Red);
                    return 1;
                }

                resultsCount = ConsoleHelper.ReadInt(
                    $"Количество результатов (10-100)",
                    config.Search.DefaultResults, 10, 100);

                depth = ConsoleHelper.ReadInt(
                    $"Глубина обхода сайта (1-{config.Search.MaxDepth})",
                    config.Search.DefaultDepth, 1, config.Search.MaxDepth);

                Console.WriteLine();
            }
            else
            {
                query = options.Query ?? string.Empty;
                resultsCount = options.Results > 0 ? options.Results : config.Search.DefaultResults;
                depth = options.Depth > 0 ? options.Depth : config.Search.DefaultDepth;
            }

            // Override config with command line options
            if (options.Delay > 0) config.Parser.Delay = options.Delay;
            if (options.Timeout > 0) config.Parser.Timeout = options.Timeout;
            if (options.Threads > 0) config.Parser.Threads = options.Threads;

            // Check if URLs file is provided (direct URL parsing mode)
            if (!string.IsNullOrWhiteSpace(options.UrlsFile))
            {
                if (File.Exists(options.UrlsFile))
                {
                    var urls = File.ReadAllLines(options.UrlsFile)
                        .Where(line => !string.IsNullOrWhiteSpace(line) && line.Trim().StartsWith("http"))
                        .Select(line => line.Trim())
                        .ToList();

                    if (urls.Any())
                    {
                        var orchestrator = new ParserOrchestrator(config, logger);
                        var results = await orchestrator.RunFromUrlsAsync(urls, depth);

                        if (results.Any())
                        {
                            var format = !string.IsNullOrWhiteSpace(options.Format) ? options.Format : config.Export.DefaultFormat;
                            orchestrator.ExportResults(results, "direct_urls", options.OutputFile, format);
                        }

                        Console.WriteLine();
                        logger.Success("Работа завершена успешно");
                        if (config.Logging.Enabled)
                        {
                            logger.Info($"Лог: {Path.GetFullPath(logFile)}");
                        }
                        return 0;
                    }
                    else
                    {
                        logger.Warning($"Файл {options.UrlsFile} не содержит валидных URL");
                    }
                }
                else
                {
                    logger.Warning($"Файл URLs не найден: {options.UrlsFile}");
                }
            }

            // Handle queries file
            var queries = new List<string>();
            if (!string.IsNullOrWhiteSpace(options.QueriesFile))
            {
                if (File.Exists(options.QueriesFile))
                {
                    queries.AddRange(File.ReadAllLines(options.QueriesFile)
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .Select(line => line.Trim()));
                }
                else
                {
                    logger.Warning($"Файл запросов не найден: {options.QueriesFile}");
                }
            }
            else if (!string.IsNullOrWhiteSpace(query))
            {
                queries.Add(query);
            }

            if (!queries.Any())
            {
                ConsoleHelper.WriteColor("Ошибка: нет запросов для обработки", ConsoleColor.Red);
                return 1;
            }

            // Determine search engine
            string? searchEngine = null;
            if (options.UseSelenium)
            {
                searchEngine = "selenium";
                logger.Info("Режим: Selenium (автоматический браузер)");
            }
            else if (options.UseGoogle)
            {
                searchEngine = "google";
                logger.Info("Режим: Google Search");
            }

            // Process each query
            foreach (var currentQuery in queries)
            {
                if (queries.Count > 1)
                {
                    logger.Info($"Обработка запроса: {currentQuery}");
                    Console.WriteLine();
                }

                var orchestrator = new ParserOrchestrator(config, logger);
                var results = await orchestrator.RunAsync(currentQuery, resultsCount, depth, searchEngine);

                if (results.Any())
                {
                    var format = !string.IsNullOrWhiteSpace(options.Format) ? options.Format : config.Export.DefaultFormat;
                    orchestrator.ExportResults(results, currentQuery, options.OutputFile, format);
                }

                Console.WriteLine();
            }

            logger.Success("Работа завершена успешно");
            if (config.Logging.Enabled)
            {
                logger.Info($"Лог: {Path.GetFullPath(logFile)}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteColor($"Критическая ошибка: {ex.Message}", ConsoleColor.Red);
            if (args.Contains("--verbose") || args.Contains("-v"))
            {
                Console.WriteLine(ex.StackTrace);
            }
            return 1;
        }
    }

    static CommandLineOptions ParseArguments(string[] args)
    {
        var options = new CommandLineOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "-q":
                case "--query":
                    if (i + 1 < args.Length) options.Query = args[++i];
                    break;

                case "-f":
                case "--queries":
                    if (i + 1 < args.Length) options.QueriesFile = args[++i];
                    break;

                case "--urls":
                    if (i + 1 < args.Length) options.UrlsFile = args[++i];
                    break;

                case "-r":
                case "--results":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var results))
                        options.Results = results;
                    break;

                case "-d":
                case "--depth":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var depth))
                        options.Depth = depth;
                    break;

                case "--delay":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var delay))
                        options.Delay = delay;
                    break;

                case "--timeout":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var timeout))
                        options.Timeout = timeout;
                    break;

                case "-o":
                case "--output":
                    if (i + 1 < args.Length) options.OutputFile = args[++i];
                    break;

                case "--format":
                    if (i + 1 < args.Length) options.Format = args[++i];
                    break;

                case "--google":
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                    {
                        // If next argument is not a flag, old behavior (Google Sheets)
                        options.GoogleSheets = true;
                        options.Format = "csv";
                    }
                    else
                    {
                        // New behavior: Google search
                        options.UseGoogle = true;
                    }
                    break;

                case "--google-search":
                    options.UseGoogle = true;
                    break;

                case "--selenium":
                    options.UseSelenium = true;
                    break;

                case "--threads":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var threads))
                        options.Threads = threads;
                    break;

                case "--config":
                    if (i + 1 < args.Length) options.ConfigFile = args[++i];
                    break;

                case "--log":
                    if (i + 1 < args.Length) options.LogFile = args[++i];
                    break;

                case "-v":
                case "--verbose":
                    options.Verbose = true;
                    break;

                case "--cleanup":
                    options.Cleanup = true;
                    break;

                case "--cleanup-all":
                    options.CleanupAll = true;
                    break;

                case "--storage-info":
                case "--info":
                    options.ShowStorage = true;
                    break;

                case "-h":
                case "--help":
                    PrintHelp();
                    Environment.Exit(0);
                    break;
            }
        }

        return options;
    }

    static void PrintHelp()
    {
        Console.WriteLine("LeadSearchParser - Парсер контактов из Яндекса");
        Console.WriteLine();
        Console.WriteLine("Использование: LeadSearchParser.exe [параметры]");
        Console.WriteLine();
        Console.WriteLine("Параметры:");
        Console.WriteLine("  -q, --query <текст>          Поисковый запрос");
        Console.WriteLine("  -f, --queries <файл>         Файл с запросами (каждый запрос на новой строке)");
        Console.WriteLine("  --urls <файл>                Файл с URL сайтов (прямой парсинг без поиска)");
        Console.WriteLine("  -r, --results <число>        Количество результатов поиска (по умолчанию: 30)");
        Console.WriteLine("  -d, --depth <число>          Глубина обхода сайта (по умолчанию: 2)");
        Console.WriteLine("  --delay <число>              Задержка между запросами в секундах (по умолчанию: 2)");
        Console.WriteLine("  --timeout <число>            Таймаут загрузки страницы в секундах (по умолчанию: 30)");
        Console.WriteLine("  -o, --output <файл>          Имя выходного файла");
        Console.WriteLine("  --format <xlsx|csv|json>     Формат вывода (по умолчанию: xlsx)");
        Console.WriteLine("  --google                     Экспорт в CSV для Google Sheets");
        Console.WriteLine("  --google-search              Использовать Google для поиска вместо Яндекса");
        Console.WriteLine("  --selenium                   Использовать браузер (обход капчи, НАДЕЖНО!)");
        Console.WriteLine("  --threads <число>            Количество потоков (по умолчанию: 3)");
        Console.WriteLine("  --config <файл>              Файл конфигурации (по умолчанию: config.json)");
        Console.WriteLine("  --log <файл>                 Файл лога");
        Console.WriteLine("  -v, --verbose                Подробный вывод");
        Console.WriteLine("  -h, --help                   Показать справку");
        Console.WriteLine();
        Console.WriteLine("Управление файлами:");
        Console.WriteLine("  --cleanup                    Удалить старые файлы (>1 дня)");
        Console.WriteLine("  --cleanup-all                Удалить ВСЕ файлы результатов");
        Console.WriteLine("  --storage-info, --info       Показать информацию о хранилище");
        Console.WriteLine();
        Console.WriteLine("Примеры:");
        Console.WriteLine("  LeadSearchParser.exe");
        Console.WriteLine("  LeadSearchParser.exe -q \"стеклянные перегородки\" -r 50");
        Console.WriteLine("  LeadSearchParser.exe -q \"окна\" --selenium         (с браузером, без капчи!)");
        Console.WriteLine("  LeadSearchParser.exe -q \"мебель\" --google-search  (поиск в Google)");
        Console.WriteLine("  LeadSearchParser.exe --urls urls.txt -d 3");
        Console.WriteLine("  LeadSearchParser.exe -f queries.txt -o results.xlsx");
    }
}

class CommandLineOptions
{
    public string? Query { get; set; }
    public string? QueriesFile { get; set; }
    public string? UrlsFile { get; set; }
    public int Results { get; set; }
    public int Depth { get; set; }
    public int Delay { get; set; }
    public int Timeout { get; set; }
    public string? OutputFile { get; set; }
    public string? Format { get; set; }
    public bool GoogleSheets { get; set; }
    public bool UseSelenium { get; set; }
    public bool UseGoogle { get; set; }
    public int Threads { get; set; }
    public string? ConfigFile { get; set; }
    public string? LogFile { get; set; }
    public bool Verbose { get; set; }
    public bool Cleanup { get; set; }
    public bool CleanupAll { get; set; }
    public bool ShowStorage { get; set; }
}

