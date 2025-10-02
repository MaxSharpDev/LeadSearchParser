namespace LeadSearchParser.Models;

public class SiteData
{
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public List<string> Emails { get; set; } = new();
    public List<string> Phones { get; set; } = new();
    public string VK { get; set; } = string.Empty;
    public string Telegram { get; set; } = string.Empty;
    public DateTime ParseDate { get; set; } = DateTime.Now;
    public bool IsSuccess { get; set; } = true;
    public string ErrorMessage { get; set; } = string.Empty;
}


