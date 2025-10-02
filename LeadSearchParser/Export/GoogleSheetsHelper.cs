using LeadSearchParser.Models;
using System.Text;

namespace LeadSearchParser.Export;

/// <summary>
/// Помощник для работы с Google Таблицами
/// </summary>
public class GoogleSheetsHelper
{
    /// <summary>
    /// Экспорт в CSV формат, оптимизированный для Google Sheets
    /// </summary>
    public void ExportForGoogleSheets(List<SiteData> data, string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var csv = new StringBuilder();

            // UTF-8 BOM для корректного отображения кириллицы
            var utf8WithBom = new UTF8Encoding(true);

            // Headers - оптимизированные для Google Sheets
            csv.AppendLine("№,Название сайта,URL,Email (все),Телефон,VK,Telegram,Дата парсинга,Статус");

            // Data
            foreach (var site in data)
            {
                var line = string.Join(",",
                    site.Number,
                    EscapeCsvForGoogle(site.Title),
                    EscapeCsvForGoogle(site.Url),
                    EscapeCsvForGoogle(string.Join("; ", site.Emails)),
                    EscapeCsvForGoogle(string.Join("; ", site.Phones)),
                    EscapeCsvForGoogle(site.VK),
                    EscapeCsvForGoogle(site.Telegram),
                    site.ParseDate.ToString("dd.MM.yyyy HH:mm:ss"),
                    site.IsSuccess ? "OK" : $"Ошибка: {EscapeCsvForGoogle(site.ErrorMessage)}"
                );

                csv.AppendLine(line);
            }

            File.WriteAllText(filePath, csv.ToString(), utf8WithBom);
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при экспорте для Google Sheets: {ex.Message}", ex);
        }
    }

    private string EscapeCsvForGoogle(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Google Sheets лучше работает с запятыми и кавычками
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    /// <summary>
    /// Генерирует инструкцию для загрузки в Google Sheets
    /// </summary>
    public string GetImportInstructions(string csvFilePath)
    {
        var instructions = new StringBuilder();
        instructions.AppendLine();
        instructions.AppendLine("╔════════════════════════════════════════════════════════════════╗");
        instructions.AppendLine("║     КАК ЗАГРУЗИТЬ В GOOGLE ТАБЛИЦЫ (Google Sheets)            ║");
        instructions.AppendLine("╚════════════════════════════════════════════════════════════════╝");
        instructions.AppendLine();
        instructions.AppendLine("📋 Файл для загрузки:");
        instructions.AppendLine($"   {Path.GetFullPath(csvFilePath)}");
        instructions.AppendLine();
        instructions.AppendLine("🔗 Пошаговая инструкция:");
        instructions.AppendLine();
        instructions.AppendLine("1. Откройте Google Sheets:");
        instructions.AppendLine("   → https://docs.google.com/spreadsheets/");
        instructions.AppendLine();
        instructions.AppendLine("2. Нажмите \"Создать\" (пустая таблица)");
        instructions.AppendLine();
        instructions.AppendLine("3. В меню выберите:");
        instructions.AppendLine("   Файл → Импорт → Загрузить");
        instructions.AppendLine();
        instructions.AppendLine("4. Перетащите CSV файл или выберите его");
        instructions.AppendLine();
        instructions.AppendLine("5. Настройки импорта:");
        instructions.AppendLine("   ✓ Разделитель: Запятая");
        instructions.AppendLine("   ✓ Кодировка: UTF-8");
        instructions.AppendLine("   ✓ Преобразовать текст в числа: ДА");
        instructions.AppendLine();
        instructions.AppendLine("6. Нажмите \"Импортировать данные\"");
        instructions.AppendLine();
        instructions.AppendLine("✅ Готово! Данные загружены в Google Таблицы");
        instructions.AppendLine();
        instructions.AppendLine("💡 Совет: Можете поделиться таблицей с коллегами через кнопку");
        instructions.AppendLine("   \"Настройки доступа\" (Share)");
        instructions.AppendLine();
        
        return instructions.ToString();
    }
}

