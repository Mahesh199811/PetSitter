namespace PetSitterConnect.Interfaces;

public interface IDatabaseService
{
    Task InitializeAsync();
    Task SeedDataAsync();
    Task<bool> DatabaseExistsAsync();
    Task DeleteDatabaseAsync();
}
