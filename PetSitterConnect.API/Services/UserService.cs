using Microsoft.EntityFrameworkCore;
using PetSitterConnect.Data;
using PetSitterConnect.Models;
using PetSitterConnect.Interfaces;

namespace PetSitterConnect.Services;

public class UserService : IUserService
{
    private readonly PetSitterDbContext _context;

    public UserService(PetSitterDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        return await _context.Users
            .Include(u => u.Pets)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.Pets)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetPetSittersAsync(double? latitude = null, double? longitude = null, double radiusKm = 50)
    {
        var query = _context.Users
            .Where(u => (u.UserType == UserType.PetSitter || u.UserType == UserType.Both) 
                       && u.IsActive && u.IsAvailable);

        if (latitude.HasValue && longitude.HasValue)
        {
            // Simple distance calculation (for more accurate results, consider using a spatial database)
            query = query.Where(u => u.Latitude.HasValue && u.Longitude.HasValue)
                        .Where(u => Math.Abs(u.Latitude!.Value - latitude.Value) <= radiusKm / 111.0 
                                   && Math.Abs(u.Longitude!.Value - longitude.Value) <= radiusKm / 111.0);
        }

        return await query
            .OrderByDescending(u => u.AverageRating)
            .ThenByDescending(u => u.TotalReviews)
            .ToListAsync();
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        try
        {
            user.LastActive = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateUserLocationAsync(string userId, double latitude, double longitude)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.Latitude = latitude;
            user.Longitude = longitude;
            user.LastActive = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateUserAvailabilityAsync(string userId, bool isAvailable)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.IsAvailable = isAvailable;
            user.LastActive = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<double> CalculateUserRatingAsync(string userId)
    {
        var reviews = await _context.Reviews
            .Where(r => r.RevieweeId == userId && r.IsVisible)
            .ToListAsync();

        if (!reviews.Any()) return 0.0;

        return Math.Round(reviews.Average(r => r.Rating), 1);
    }

    public async Task<bool> UpdateUserRatingAsync(string userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            var reviews = await _context.Reviews
                .Where(r => r.RevieweeId == userId && r.IsVisible)
                .ToListAsync();

            if (reviews.Any())
            {
                user.AverageRating = Math.Round(reviews.Average(r => r.Rating), 1);
                user.TotalReviews = reviews.Count;
            }
            else
            {
                user.AverageRating = 0.0;
                user.TotalReviews = 0;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, UserType? userType = null)
    {
        var query = _context.Users.Where(u => u.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(u => u.FirstName.ToLower().Contains(searchTerm) ||
                                    u.LastName.ToLower().Contains(searchTerm) ||
                                    u.City!.ToLower().Contains(searchTerm) ||
                                    u.Bio!.ToLower().Contains(searchTerm));
        }

        if (userType.HasValue)
        {
            query = query.Where(u => u.UserType == userType.Value || u.UserType == UserType.Both);
        }

        return await query
            .OrderByDescending(u => u.AverageRating)
            .ThenByDescending(u => u.TotalReviews)
            .ToListAsync();
    }
}
