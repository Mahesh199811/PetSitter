namespace PetSitterConnect.Models;

public class SearchAnalytics
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string? SearchTerm { get; set; }
    public string? Location { get; set; }
    public string? ServiceType { get; set; }
    public int ResultCount { get; set; }
    public DateTime SearchedAt { get; set; }
}