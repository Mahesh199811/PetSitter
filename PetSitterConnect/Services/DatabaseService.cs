using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using PetSitterConnect.Data;
using PetSitterConnect.Models;

namespace PetSitterConnect.Services;

public class DatabaseService : IDatabaseService
{
    private readonly PetSitterDbContext _context;
    private readonly UserManager<User> _userManager;

    public DatabaseService(
        PetSitterDbContext context,
        UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Check if database exists
            var exists = await _context.Database.CanConnectAsync();

            if (!exists)
            {
                // Create database with all tables
                await _context.Database.EnsureCreatedAsync();
                System.Diagnostics.Debug.WriteLine("Database created successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Database already exists");
            }

            // Seed initial data
            await SeedDataAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");

            // If there's an error, try to recreate the database
            try
            {
                await _context.Database.EnsureDeletedAsync();
                await _context.Database.EnsureCreatedAsync();
                await SeedDataAsync();
                System.Diagnostics.Debug.WriteLine("Database recreated successfully");
            }
            catch (Exception recreateEx)
            {
                System.Diagnostics.Debug.WriteLine($"Database recreation failed: {recreateEx.Message}");
                throw;
            }
        }
    }

    public async Task SeedDataAsync()
    {
        try
        {
            // Create a default admin user if no users exist
            if (!await _context.Users.AnyAsync())
            {
                await CreateDefaultAdminUserAsync();
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Data seeding error: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DatabaseExistsAsync()
    {
        try
        {
            return await _context.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }

    public async Task DeleteDatabaseAsync()
    {
        try
        {
            await _context.Database.EnsureDeletedAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database deletion error: {ex.Message}");
            throw;
        }
    }

    private async Task CreateDefaultAdminUserAsync()
    {
        var adminUser = new User
        {
            UserName = "admin@petsitterconnect.com",
            Email = "admin@petsitterconnect.com",
            FirstName = "Admin",
            LastName = "User",
            UserType = UserType.Both,
            IsActive = true,
            IsVerified = true,
            DateJoined = DateTime.UtcNow,
            LastActive = DateTime.UtcNow,
            City = "Demo City",
            Country = "Demo Country"
        };

        var result = await _userManager.CreateAsync(adminUser, "Admin123!");

        if (result.Succeeded)
        {
            System.Diagnostics.Debug.WriteLine("Default admin user created successfully");

            // Create some test pets for the admin user
            await CreateTestPetsAsync(adminUser.Id);
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            System.Diagnostics.Debug.WriteLine($"Failed to create admin user: {errors}");
        }
    }

    private async Task CreateTestPetsAsync(string userId)
    {
        var testPets = new List<Pet>
        {
            new Pet
            {
                Name = "Buddy",
                Type = PetType.Dog,
                Breed = "Golden Retriever",
                Age = 3,
                Gender = "Male",
                Weight = 30.5,
                Size = "Large",
                Description = "Friendly and energetic dog who loves to play fetch",
                SpecialNeeds = "Needs daily exercise",
                IsVaccinated = true,
                IsFriendlyWithOtherPets = true,
                IsFriendlyWithChildren = true,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Pet
            {
                Name = "Whiskers",
                Type = PetType.Cat,
                Breed = "Persian",
                Age = 2,
                Gender = "Female",
                Weight = 4.2,
                Size = "Medium",
                Description = "Calm and affectionate cat who loves to cuddle",
                SpecialNeeds = "Indoor only, needs daily brushing",
                IsVaccinated = true,
                IsFriendlyWithOtherPets = false,
                IsFriendlyWithChildren = true,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Pet
            {
                Name = "Max",
                Type = PetType.Dog,
                Breed = "Labrador",
                Age = 5,
                Gender = "Male",
                Weight = 28.0,
                Size = "Large",
                Description = "Well-trained and calm dog, great with kids",
                SpecialNeeds = "Needs medication twice daily",
                MedicalConditions = "Mild arthritis",
                Medications = "Joint supplements",
                IsVaccinated = true,
                IsFriendlyWithOtherPets = true,
                IsFriendlyWithChildren = true,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.Pets.AddRange(testPets);
        await _context.SaveChangesAsync();

        System.Diagnostics.Debug.WriteLine($"Created {testPets.Count} test pets for user {userId}");
    }
}
