using System.Text.RegularExpressions;

namespace LeadSearchParser.Core;

public class FakeDataGenerator
{
    private static readonly Random _random = new Random();
    
    // Списки для генерации реалистичных данных
    private static readonly string[] _companyNames = {
        "ООО \"СтройКом\"", "ИП Иванов А.В.", "ООО \"МеталлПро\"", "ЗАО \"СтеклоМир\"",
        "ООО \"МебельГрад\"", "ИП Петров С.И.", "ООО \"ОкнаПлюс\"", "ЗАО \"ДверьСервис\"",
        "ООО \"ПотолокМастер\"", "ИП Сидоров М.А.", "ООО \"СантехПро\"", "ЗАО \"ЭлектроСвет\"",
        "ООО \"КровляСтрой\"", "ИП Козлов В.П.", "ООО \"ФасадТек\"", "ЗАО \"ПаркетМир\"",
        "ООО \"БетонСервис\"", "ИП Морозов А.С.", "ООО \"ИзоляцияПро\"", "ЗАО \"Ландшафт\""
    };

    private static readonly string[] _domains = {
        "stroykom.ru", "metallpro.com", "steklomir.ru", "mebelgrad.com",
        "oknaplus.ru", "dverservis.com", "potolok-master.ru", "santehpro.com",
        "elektrosvet.ru", "krovlastroy.com", "fasadtek.ru", "parketmir.com",
        "betonservis.ru", "izolyaciya-pro.com", "landshaft-design.ru", "remont-plus.com",
        "stroyka-master.ru", "dizain-home.ru", "stroymaterial.ru", "proekt-dom.ru"
    };

    private static readonly string[] _businessTypes = {
        "стеклянные перегородки", "металлоконструкции", "окна ПВХ", "двери входные",
        "натяжные потолки", "сантехника", "электромонтаж", "кровельные работы",
        "фасадные работы", "паркетные работы", "бетонные работы", "теплоизоляция",
        "ландшафтный дизайн", "ремонт квартир", "строительство домов", "дизайн интерьеров"
    };

    private static readonly string[] _phonePrefixes = {
        "+7 (495)", "+7 (499)", "+7 (812)", "+7 (831)", "+7 (383)", "+7 (343)", 
        "+7 (391)", "+7 (846)", "+7 (3532)", "+7 (4212)", "+7 (423)", "+7 (3513)",
        "+7 (8622)", "+7 (8512)", "+7 (8652)", "+7 (863)", "+7 (861)", "+7 (8352)",
        "+7 (8313)", "+7 (8312)", "+7 (8319)", "+7 (8353)", "+7 (8314)", "+7 (8315)"
    };

    private static readonly string[] _emailDomains = {
        "mail.ru", "yandex.ru", "gmail.com", "rambler.ru", "bk.ru", "inbox.ru",
        "list.ru", "yahoo.com", "hotmail.com", "outlook.com", "icloud.com"
    };

    private static readonly string[] _vkProfiles = {
        "vk.com/stroykom", "vk.com/metallpro", "vk.com/steklomir", "vk.com/mebelgrad",
        "vk.com/oknaplus", "vk.com/dverservis", "vk.com/potolok", "vk.com/santehpro",
        "vk.com/elektrosvet", "vk.com/krovlastroy", "vk.com/fasadtek", "vk.com/parketmir",
        "vk.com/betonservis", "vk.com/izolyaciya", "vk.com/landshaft", "vk.com/remontplus"
    };

    private static readonly string[] _telegramProfiles = {
        "@stroykom", "@metallpro", "@steklomir", "@mebelgrad", "@oknaplus", "@dverservis",
        "@potolok_master", "@santehpro", "@elektrosvet", "@krovlastroy", "@fasadtek",
        "@parketmir", "@betonservis", "@izolyaciya_pro", "@landshaft_design", "@remont_plus"
    };

    public static string GenerateCompanyName(string query)
    {
        // Генерируем название компании на основе запроса
        var businessType = _businessTypes.FirstOrDefault(bt => 
            bt.ToLower().Contains(query.ToLower()) || query.ToLower().Contains(bt.ToLower()));
        
        if (businessType != null)
        {
            var companyName = _companyNames[_random.Next(_companyNames.Length)];
            return $"{companyName} - {businessType}";
        }
        
        return _companyNames[_random.Next(_companyNames.Length)];
    }

    public static string GenerateDomain(string query)
    {
        // Генерируем домен на основе запроса
        var businessType = _businessTypes.FirstOrDefault(bt => 
            bt.ToLower().Contains(query.ToLower()) || query.ToLower().Contains(bt.ToLower()));
        
        if (businessType != null)
        {
            var baseDomain = _domains[_random.Next(_domains.Length)];
            var queryWords = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var randomWord = queryWords[_random.Next(queryWords.Length)];
            return $"{randomWord}-{baseDomain}";
        }
        
        return _domains[_random.Next(_domains.Length)];
    }

    public static string GeneratePhone()
    {
        var prefix = _phonePrefixes[_random.Next(_phonePrefixes.Length)];
        var number = _random.Next(100, 999).ToString();
        var number2 = _random.Next(10, 99).ToString();
        var number3 = _random.Next(10, 99).ToString();
        
        return $"{prefix} {number}-{number2}-{number3}";
    }

    public static string GenerateEmail(string? companyName = null)
    {
        var domain = _emailDomains[_random.Next(_emailDomains.Length)];
        
        if (!string.IsNullOrEmpty(companyName))
        {
            // Генерируем email на основе названия компании
            var cleanName = Regex.Replace(companyName.ToLower(), @"[^\w]", "");
            cleanName = cleanName.Substring(0, Math.Min(cleanName.Length, 8));
            
            var variants = new[]
            {
                $"{cleanName}@{domain}",
                $"info@{cleanName}.ru",
                $"sales@{cleanName}.ru",
                $"manager@{cleanName}.ru"
            };
            
            return variants[_random.Next(variants.Length)];
        }
        
        var names = new[] { "info", "sales", "manager", "admin", "contact", "support" };
        var name = names[_random.Next(names.Length)];
        var randomNum = _random.Next(1, 99);
        
        return $"{name}{randomNum}@{domain}";
    }

    public static string GenerateVKProfile()
    {
        return _vkProfiles[_random.Next(_vkProfiles.Length)];
    }

    public static string GenerateTelegramProfile()
    {
        return _telegramProfiles[_random.Next(_telegramProfiles.Length)];
    }

    public static List<string> GeneratePhoneList(int count = 1)
    {
        var phones = new HashSet<string>();
        
        while (phones.Count < count)
        {
            phones.Add(GeneratePhone());
        }
        
        return phones.ToList();
    }

    public static List<string> GenerateEmailList(string? companyName = null, int count = 1)
    {
        var emails = new HashSet<string>();
        
        while (emails.Count < count)
        {
            emails.Add(GenerateEmail(companyName));
        }
        
        return emails.ToList();
    }

    public static string GenerateTitle(string query, string domain)
    {
        var businessType = _businessTypes.FirstOrDefault(bt => 
            bt.ToLower().Contains(query.ToLower()) || query.ToLower().Contains(bt.ToLower()));
        
        if (businessType != null)
        {
            var actions = new[] { "Продажа", "Изготовление", "Установка", "Монтаж", "Производство" };
            var quality = new[] { "качественные", "надежные", "современные", "профессиональные" };
            var action = actions[_random.Next(actions.Length)];
            var qual = quality[_random.Next(quality.Length)];
            
            return $"{action} {qual} {businessType} - {domain}";
        }
        
        var genericTitles = new[]
        {
            $"Строительные услуги - {domain}",
            $"Профессиональный ремонт - {domain}",
            $"Качественные материалы - {domain}",
            $"Строительная компания - {domain}",
            $"Ремонт и отделка - {domain}"
        };
        
        return genericTitles[_random.Next(genericTitles.Length)];
    }

    public static List<string> GenerateSearchUrls(string query, int count)
    {
        var urls = new HashSet<string>();
        var businessType = _businessTypes.FirstOrDefault(bt => 
            bt.ToLower().Contains(query.ToLower()) || query.ToLower().Contains(bt.ToLower()));
        
        while (urls.Count < count)
        {
            var domain = GenerateDomain(query);
            var protocol = _random.Next(2) == 0 ? "https://" : "http://";
            var www = _random.Next(3) == 0 ? "www." : "";
            
            var pathVariants = new[]
            {
                "",
                "/",
                "/index.html",
                "/about",
                "/services",
                "/contact",
                "/catalog",
                "/price",
                "/portfolio"
            };
            
            var path = pathVariants[_random.Next(pathVariants.Length)];
            var url = $"{protocol}{www}{domain}{path}";
            urls.Add(url);
        }
        
        return urls.ToList();
    }

    public static void AddRandomDelay()
    {
        // Имитируем реальные задержки парсинга
        var delay = _random.Next(800, 2500); // 0.8-2.5 секунды
        Thread.Sleep(delay);
    }

    public static int GetRandomSuccessRate()
    {
        // Возвращаем процент успешных результатов (70-95%)
        return _random.Next(70, 96);
    }

    public static bool ShouldGenerateContacts()
    {
        // 85% шанс найти контакты на сайте
        return _random.Next(100) < 85;
    }

    public static int GetRandomContactCount(int max = 5)
    {
        // Возвращает количество контактов (1-5)
        return _random.Next(1, max + 1);
    }
}
