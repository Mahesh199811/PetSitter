using PetSitterConnect.Models;

namespace PetSitterConnect.Services;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(string userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<IEnumerable<User>> GetPetSittersAsync(double? latitude = null, double? longitude = null, double radiusKm = 50);
    Task<bool> UpdateUserAsync(User user);
    Task<bool> UpdateUserLocationAsync(string userId, double latitude, double longitude);
    Task<bool> UpdateUserAvailabilityAsync(string userId, bool isAvailable);
    Task<double> CalculateUserRatingAsync(string userId);
    Task<bool> UpdateUserRatingAsync(string userId);
    Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, UserType? userType = null);
}
