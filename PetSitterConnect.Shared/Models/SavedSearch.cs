namespace PetSitterConnect.Models;

public class SavedSearch
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Criteria { get; set; } = string.Empty; // JSON serialized SearchCriteria
    public DateTime CreatedAt { get; set; }
}