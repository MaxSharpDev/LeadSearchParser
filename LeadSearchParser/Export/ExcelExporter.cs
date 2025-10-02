using ClosedXML.Excel;
using LeadSearchParser.Models;

namespace LeadSearchParser.Export;

public class ExcelExporter
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

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Результаты");

            // Headers
            worksheet.Cell(1, 1).Value = "№";
            worksheet.Cell(1, 2).Value = "Название сайта";
            worksheet.Cell(1, 3).Value = "URL";
            worksheet.Cell(1, 4).Value = "Email (все)";
            worksheet.Cell(1, 5).Value = "Телефон";
            worksheet.Cell(1, 6).Value = "VK";
            worksheet.Cell(1, 7).Value = "Telegram";
            worksheet.Cell(1, 8).Value = "Дата парсинга";
            worksheet.Cell(1, 9).Value = "Статус";

            // Style headers
            var headerRange = worksheet.Range(1, 1, 1, 9);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Data
            int row = 2;
            foreach (var site in data)
            {
                worksheet.Cell(row, 1).Value = site.Number;
                worksheet.Cell(row, 2).Value = site.Title;
                worksheet.Cell(row, 3).Value = site.Url;
                
                // Email в столбик (с переносом строки)
                var emailCell = worksheet.Cell(row, 4);
                emailCell.Value = string.Join(Environment.NewLine, site.Emails);
                emailCell.Style.Alignment.WrapText = true;
                emailCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                
                // Телефоны в столбик (с переносом строки)
                var phoneCell = worksheet.Cell(row, 5);
                phoneCell.Value = string.Join(Environment.NewLine, site.Phones);
                phoneCell.Style.Alignment.WrapText = true;
                phoneCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                
                worksheet.Cell(row, 6).Value = site.VK;
                worksheet.Cell(row, 7).Value = site.Telegram;
                worksheet.Cell(row, 8).Value = site.ParseDate.ToString("dd.MM.yyyy HH:mm:ss");
                worksheet.Cell(row, 9).Value = site.IsSuccess ? "OK" : $"Ошибка: {site.ErrorMessage}";

                // Color code status
                if (!site.IsSuccess)
                {
                    worksheet.Cell(row, 9).Style.Font.FontColor = XLColor.Red;
                }
                else if (site.Emails.Any() || site.Phones.Any())
                {
                    worksheet.Cell(row, 9).Style.Font.FontColor = XLColor.Green;
                }

                // Увеличиваем высоту строки если много данных
                var maxItems = Math.Max(site.Emails.Count, site.Phones.Count);
                if (maxItems > 1)
                {
                    worksheet.Row(row).Height = Math.Min(15 * maxItems, 200); // Макс 200
                }

                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Set maximum column widths for better readability
            foreach (var column in worksheet.ColumnsUsed())
            {
                if (column.Width > 50)
                {
                    column.Width = 50;
                }
            }

            workbook.SaveAs(filePath);
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при экспорте в Excel: {ex.Message}", ex);
        }
    }
}


