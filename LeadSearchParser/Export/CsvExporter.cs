using System.Text;
using LeadSearchParser.Models;

namespace LeadSearchParser.Export;

public class CsvExporter
{
    public void Export(List<SiteData> data, string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var csv = new StringBuilder();

            // Headers
            csv.AppendLine("№;Название сайта;URL;Email (все);Телефон;VK;Telegram;Дата парсинга;Статус");

            // Data
            foreach (var site in data)
            {
                var line = string.Join(";",
                    site.Number,
                    EscapeCsv(site.Title),
                    EscapeCsv(site.Url),
                    EscapeCsv(string.Join(", ", site.Emails)),
                    EscapeCsv(string.Join(", ", site.Phones)),
                    EscapeCsv(site.VK),
                    EscapeCsv(site.Telegram),
                    site.ParseDate.ToString("dd.MM.yyyy HH:mm:ss"),
                    site.IsSuccess ? "OK" : $"Ошибка: {site.ErrorMessage}"
                );

                csv.AppendLine(line);
            }

            File.WriteAllText(filePath, csv.ToString(), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при экспорте в CSV: {ex.Message}", ex);
        }
    }

    private string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(";") || value.Contains("\"") || value.Contains("\n"))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}


