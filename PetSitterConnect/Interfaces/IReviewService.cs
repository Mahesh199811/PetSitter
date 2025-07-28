using PetSitterConnect.Models;

namespace PetSitterConnect.Interfaces;

public interface IReviewService
{
    // Create and manage reviews
    Task<Review> CreateReviewAsync(Review review);
    Task<Review?> GetReviewAsync(int reviewId);
    Task<bool> UpdateReviewAsync(Review review);
    Task<bool> DeleteReviewAsync(int reviewId);

    // Get reviews for a user
    Task<IEnumerable<Review>> GetReviewsForUserAsync(string userId, ReviewType? reviewType = null);
    Task<IEnumerable<Review>> GetReviewsByUserAsync(string userId, ReviewType? reviewType = null);

    // Get reviews for a booking
    Task<IEnumerable<Review>> GetReviewsForBookingAsync(int bookingId);
    Task<Review?> GetReviewForBookingAsync(int bookingId, string reviewerId);

    // Check if user can review
    Task<bool> CanUserReviewBookingAsync(int bookingId, string userId);
    Task<bool> HasUserReviewedBookingAsync(int bookingId, string userId);

    // Rating calculations
    Task<double> CalculateAverageRatingAsync(string userId, ReviewType? reviewType = null);
    Task<Dictionary<int, int>> GetRatingDistributionAsync(string userId, ReviewType? reviewType = null);
    Task<bool> UpdateUserRatingAsync(string userId);

    // Review statistics
    Task<int> GetTotalReviewsCountAsync(string userId, ReviewType? reviewType = null);
    Task<IEnumerable<Review>> GetRecentReviewsAsync(string userId, int count = 5);

    // Review validation
    Task<bool> ValidateReviewAsync(Review review);
    Task<IEnumerable<Review>> GetPendingReviewsAsync(string userId);
}