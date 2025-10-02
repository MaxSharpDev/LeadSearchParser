using System.Text;

namespace LeadSearchParser.Utils;

public static class ConsoleHelper
{
    private static readonly object ConsoleLock = new();

    public static void WriteHeader()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== LeadSearchParser - Парсер контактов из Яндекса ===");
        Console.WriteLine();
        Console.ResetColor();
    }

    public static void WriteColor(string message, ConsoleColor color)
    {
        lock (ConsoleLock)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }

    public static void WriteProgress(int current, int total, string? message = null)
    {
        lock (ConsoleLock)
        {
            var percentage = (int)((double)current / total * 100);
            var progressBarLength = 50;
            var filledLength = (int)(progressBarLength * current / total);
            
            var progressBar = new string('=', filledLength) + new string(' ', progressBarLength - filledLength);
            
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write($"[{progressBar}] {percentage}% ({current}/{total})");
            
            if (!string.IsNullOrEmpty(message))
            {
                Console.Write($" - {message}");
            }
            
            if (current == total)
            {
                Console.WriteLine();
            }
        }
    }

    public static void WriteStatistics(int processed, int total, int emailsFound, int phonesFound, 
        int socialFound, int success, int errors, TimeSpan elapsed)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Статистика:");
        Console.WriteLine($"├─ Обработано сайтов: {processed}/{total}");
        Console.WriteLine($"├─ Найдено email: {emailsFound}");
        Console.WriteLine($"├─ Найдено телефонов: {phonesFound}");
        Console.WriteLine($"├─ Найдено соцсетей: {socialFound}");
        Console.WriteLine($"├─ Успешно: {success}");
        Console.WriteLine($"├─ Ошибок: {errors}");
        Console.WriteLine($"└─ Время: {elapsed:hh\\:mm\\:ss}");
        Console.ResetColor();
        Console.WriteLine();
    }

    public static string ReadInput(string prompt, string? defaultValue = null)
    {
        if (!string.IsNullOrEmpty(defaultValue))
        {
            Console.Write($"{prompt} [{defaultValue}]: ");
        }
        else
        {
            Console.Write($"{prompt}: ");
        }

        var input = Console.ReadLine();
        return string.IsNullOrWhiteSpace(input) ? (defaultValue ?? string.Empty) : input;
    }

    public static int ReadInt(string prompt, int defaultValue, int min = 1, int max = int.MaxValue)
    {
        while (true)
        {
            Console.Write($"{prompt} [{defaultValue}]: ");
            var input = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(input))
                return defaultValue;

            if (int.TryParse(input, out var value) && value >= min && value <= max)
                return value;

            WriteColor($"Введите число от {min} до {max}", ConsoleColor.Red);
        }
    }
}


