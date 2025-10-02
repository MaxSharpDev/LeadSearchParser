using LeadSearchParser.Models;

namespace LeadSearchParser.Core;

public class DataExporter
{
    private readonly Export.ExcelExporter _excelExporter = new();
    private readonly Export.CsvExporter _csvExporter = new();
    private readonly Export.JsonExporter _jsonExporter = new();

    public void Export(List<SiteData> data, string filePath, string format)
    {
        format = format.ToLower();

        switch (format)
        {
            case "xlsx":
            case "excel":
                _excelExporter.Export(data, filePath);
                break;

            case "csv":
                _csvExporter.Export(data, filePath);
                break;

            case "json":
                _jsonExporter.Export(data, filePath);
                break;

            default:
                throw new ArgumentException($"Неподдерживаемый формат: {format}. Используйте: xlsx, csv или json");
        }
    }

    public string GenerateFileName(string query, string format, string template, string outputFolder)
    {
        var date = DateTime.Now.ToString("ddMMyyyy_HHmmss");
        var safeName = template
            .Replace("{date}", date)
            .Replace("{query}", SanitizeFileName(query));

        if (!safeName.EndsWith($".{format}"))
        {
            safeName += $".{format}";
        }

        return Path.Combine(outputFolder, safeName);
    }

    private string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "query";

        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        
        if (sanitized.Length > 50)
        {
            sanitized = sanitized.Substring(0, 50);
        }

        return sanitized;
    }
}


