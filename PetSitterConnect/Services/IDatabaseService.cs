namespace PetSitterConnect.Services;

public interface IDatabaseService
{
    Task InitializeAsync();
    Task SeedDataAsync();
    Task<bool> DatabaseExistsAsync();
    Task DeleteDatabaseAsync();
}
