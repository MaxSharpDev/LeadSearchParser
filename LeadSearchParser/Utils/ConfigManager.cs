using LeadSearchParser.Models;
using Newtonsoft.Json;

namespace LeadSearchParser.Utils;

public class ConfigManager
{
    private const string DefaultConfigFile = "config.json";

    public static ParserConfig LoadConfig(string? configFile = null)
    {
        configFile ??= DefaultConfigFile;

        if (File.Exists(configFile))
        {
            try
            {
                var json = File.ReadAllText(configFile);
                var config = JsonConvert.DeserializeObject<ParserConfig>(json);
                return config ?? GetDefaultConfig();
            }
            catch
            {
                return GetDefaultConfig();
            }
        }

        return GetDefaultConfig();
    }

    public static ParserConfig GetDefaultConfig()
    {
        return new ParserConfig
        {
            Search = new SearchConfig
            {
                DefaultResults = 30,
                DefaultDepth = 2,
                MaxDepth = 5
            },
            Parser = new ParserSettings
            {
                Timeout = 30,
                Delay = 2,
                Threads = 3,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
                FollowRobotsTxt = false
            },
            Extractor = new ExtractorSettings
            {
                EmailPattern = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
                PhonePatterns = new List<string>
                {
                    @"\+7\s?\(?\d{3}\)?\s?\d{3}-?\d{2}-?\d{2}",
                    @"8\s?\(?\d{3}\)?\s?\d{3}-?\d{2}-?\d{2}"
                },
                ContactPages = new List<string> { "контакты", "contact", "contacts", "о-нас", "about", "o-nas" }
            },
            Social = new SocialSettings
            {
                VK = "vk.com",
                Telegram = "t.me|telegram.me",
                WhatsApp = "wa.me|api.whatsapp.com",
                Instagram = "instagram.com",
                Facebook = "facebook.com",
                OK = "ok.ru",
                YouTube = "youtube.com"
            },
            Export = new ExportSettings
            {
                DefaultFormat = "xlsx",
                OutputFolder = "Results",
                FilenameTemplate = "results_{date}_{query}"
            },
            Logging = new LoggingSettings
            {
                Enabled = true,
                Level = "Info",
                File = "Logs/parser.log"
            }
        };
    }

    public static void SaveConfig(ParserConfig config, string? configFile = null)
    {
        configFile ??= DefaultConfigFile;
        var json = JsonConvert.SerializeObject(config, Formatting.Indented);
        File.WriteAllText(configFile, json);
    }
}


