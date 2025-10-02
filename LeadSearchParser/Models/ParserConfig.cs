namespace LeadSearchParser.Models;

public class ParserConfig
{
    public SearchConfig Search { get; set; } = new();
    public ParserSettings Parser { get; set; } = new();
    public ExtractorSettings Extractor { get; set; } = new();
    public SocialSettings Social { get; set; } = new();
    public ExportSettings Export { get; set; } = new();
    public CleanupSettings Cleanup { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
}

public class SearchConfig
{
    public int DefaultResults { get; set; } = 30;
    public int DefaultDepth { get; set; } = 2;
    public int MaxDepth { get; set; } = 5;
    public string Engine { get; set; } = "yandex"; // yandex, google, selenium
    public bool UseSelenium { get; set; } = false;
}

public class ParserSettings
{
    public int Timeout { get; set; } = 30;
    public int Delay { get; set; } = 2;
    public int Threads { get; set; } = 3;
    public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36";
    public bool FollowRobotsTxt { get; set; } = false;
}

public class ExtractorSettings
{
    public string EmailPattern { get; set; } = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}";
    public List<string> PhonePatterns { get; set; } = new()
    {
        @"\+7\s?\(?\d{3}\)?\s?\d{3}-?\d{2}-?\d{2}",
        @"8\s?\(?\d{3}\)?\s?\d{3}-?\d{2}-?\d{2}"
    };
    public List<string> ContactPages { get; set; } = new() { "контакты", "contact", "contacts", "о-нас", "about", "o-nas" };
}

public class SocialSettings
{
    public string VK { get; set; } = "vk.com";
    public string Telegram { get; set; } = "t.me|telegram.me";
    public string WhatsApp { get; set; } = "wa.me|api.whatsapp.com";
    public string Instagram { get; set; } = "instagram.com";
    public string Facebook { get; set; } = "facebook.com";
    public string OK { get; set; } = "ok.ru";
    public string YouTube { get; set; } = "youtube.com";
}

public class ExportSettings
{
    public string DefaultFormat { get; set; } = "xlsx";
    public string OutputFolder { get; set; } = "Results";
    public string FilenameTemplate { get; set; } = "results_{date}_{query}";
}

public class CleanupSettings
{
    public bool Enabled { get; set; } = true;
    public int KeepDays { get; set; } = 1;
    public bool AutoCleanup { get; set; } = true;
}

public class LoggingSettings
{
    public bool Enabled { get; set; } = true;
    public string Level { get; set; } = "Info";
    public string File { get; set; } = "Logs/parser.log";
}


