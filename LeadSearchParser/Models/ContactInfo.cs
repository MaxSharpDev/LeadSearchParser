namespace LeadSearchParser.Models;

public class ContactInfo
{
    public List<string> Emails { get; set; } = new();
    public List<string> Phones { get; set; } = new();
    public Dictionary<string, string> SocialMedia { get; set; } = new();
}


