using System.Text;
using LeadSearchParser.Models;
using Newtonsoft.Json;

namespace LeadSearchParser.Export;

public class JsonExporter
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

            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при экспорте в JSON: {ex.Message}", ex);
        }
    }
}


