using Microsoft.EntityFrameworkCore;
using PetSitterConnect.Data;
using PetSitterConnect.Interfaces;
using PetSitterConnect.Models;

namespace PetSitterConnect.Services;

public class ReviewService : IReviewService
{
    private readonly PetSitterDbContext _context;
    private readonly IUserService _userService;
    private readonly INotificationService _notificationService;

    public ReviewService(PetSitterDbContext context, IUserService userService, INotificationService notificationService)
    {
        _context = context;
        _userService = userService;
        _notificationService = notificationService;
    }

    public async Task<Review> CreateReviewAsync(Review review)
    {
        // Validate the review
        if (!await ValidateReviewAsync(review))
        {
            throw new InvalidOperationException("Invalid review data");
        }

        // Check if user can review this booking
        if (!await CanUserReviewBookingAsync(review.BookingId, review.ReviewerId))
        {
            throw new InvalidOperationException("User cannot review this booking");
        }

        // Check if user has already reviewed this booking
        if (await HasUserReviewedBookingAsync(review.BookingId, review.ReviewerId))
        {
            throw new InvalidOperationException("User has already reviewed this booking");
        }

        review.CreatedAt = DateTime.UtcNow;
        review.UpdatedAt = DateTime.UtcNow;

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        // Update the reviewee's rating
        await UpdateUserRatingAsync(review.RevieweeId);

        // Send notification to the reviewee
        await _notificationService.SendReviewReceivedNotificationAsync(review);

        return review;
    }

    public async Task<Review?> GetReviewAsync(int reviewId)
    {
        return await _context.Reviews
            .Include(r => r.Reviewer)
            .Include(r => r.Reviewee)
            .Include(r => r.Booking)
                .ThenInclude(b => b.PetCareRequest)
                    .ThenInclude(pcr => pcr.Pet)
            .FirstOrDefaultAsync(r => r.Id == reviewId);
    }

    public async Task<bool> UpdateReviewAsync(Review review)
    {
        var existingReview = await _context.Reviews.FindAsync(review.Id);
        if (existingReview == null)
            return false;

        // Only allow the reviewer to update their own review
        if (existingReview.ReviewerId != review.ReviewerId)
            return false;

        // Update allowed fields
        existingReview.Rating = review.Rating;
        existingReview.Comment = review.Comment;
        existingReview.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Update the reviewee's rating
        await UpdateUserRatingAsync(existingReview.RevieweeId);

        return true;
    }

    public async Task<bool> DeleteReviewAsync(int reviewId)
    {
        var review = await _context.Reviews.FindAsync(reviewId);
        if (review == null)
            return false;

        var revieweeId = review.RevieweeId;
        
        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        // Update the reviewee's rating
        await UpdateUserRatingAsync(revieweeId);

        return true;
    }

    public async Task<IEnumerable<Review>> GetReviewsForUserAsync(string userId, ReviewType? reviewType = null)
    {
        var query = _context.Reviews
            .Include(r => r.Reviewer)
            .Include(r => r.Booking)
                .ThenInclude(b => b.PetCareRequest)
                    .ThenInclude(pcr => pcr.Pet)
            .Where(r => r.RevieweeId == userId && r.IsVisible);

        if (reviewType.HasValue)
        {
            query = query.Where(r => r.ReviewType == reviewType.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Review>> GetReviewsByUserAsync(string userId, ReviewType? reviewType = null)
    {
        var query = _context.Reviews
            .Include(r => r.Reviewee)
            .Include(r => r.Booking)
                .ThenInclude(b => b.PetCareRequest)
                    .ThenInclude(pcr => pcr.Pet)
            .Where(r => r.ReviewerId == userId);

        if (reviewType.HasValue)
        {
            query = query.Where(r => r.ReviewType == reviewType.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Review>> GetReviewsForBookingAsync(int bookingId)
    {
        return await _context.Reviews
            .Include(r => r.Reviewer)
            .Include(r => r.Reviewee)
            .Where(r => r.BookingId == bookingId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Review?> GetReviewForBookingAsync(int bookingId, string reviewerId)
    {
        return await _context.Reviews
            .Include(r => r.Reviewee)
            .FirstOrDefaultAsync(r => r.BookingId == bookingId && r.ReviewerId == reviewerId);
    }

    public async Task<bool> CanUserReviewBookingAsync(int bookingId, string userId)
    {
        var booking = await _context.Bookings
            .Include(b => b.PetCareRequest)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
            return false;

        // Only allow reviews after booking is completed
        if (booking.Status != BookingStatus.Completed)
            return false;

        // User must be either the owner or the sitter
        return booking.OwnerId == userId || booking.SitterId == userId;
    }

    public async Task<bool> HasUserReviewedBookingAsync(int bookingId, string userId)
    {
        return await _context.Reviews
            .AnyAsync(r => r.BookingId == bookingId && r.ReviewerId == userId);
    }

    public async Task<double> CalculateAverageRatingAsync(string userId, ReviewType? reviewType = null)
    {
        var query = _context.Reviews
            .Where(r => r.RevieweeId == userId && r.IsVisible);

        if (reviewType.HasValue)
        {
            query = query.Where(r => r.ReviewType == reviewType.Value);
        }

        var reviews = await query.ToListAsync();
        
        if (!reviews.Any())
            return 0.0;

        return Math.Round(reviews.Average(r => r.Rating), 1);
    }

    public async Task<Dictionary<int, int>> GetRatingDistributionAsync(string userId, ReviewType? reviewType = null)
    {
        var query = _context.Reviews
            .Where(r => r.RevieweeId == userId && r.IsVisible);

        if (reviewType.HasValue)
        {
            query = query.Where(r => r.ReviewType == reviewType.Value);
        }

        var ratings = await query.Select(r => r.Rating).ToListAsync();
        
        var distribution = new Dictionary<int, int>();
        for (int i = 1; i <= 5; i++)
        {
            distribution[i] = ratings.Count(r => r == i);
        }

        return distribution;
    }

    public async Task<bool> UpdateUserRatingAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        var averageRating = await CalculateAverageRatingAsync(userId);
        var totalReviews = await GetTotalReviewsCountAsync(userId);

        user.AverageRating = averageRating;
        user.TotalReviews = totalReviews;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetTotalReviewsCountAsync(string userId, ReviewType? reviewType = null)
    {
        var query = _context.Reviews
            .Where(r => r.RevieweeId == userId && r.IsVisible);

        if (reviewType.HasValue)
        {
            query = query.Where(r => r.ReviewType == reviewType.Value);
        }

        return await query.CountAsync();
    }

    public async Task<IEnumerable<Review>> GetRecentReviewsAsync(string userId, int count = 5)
    {
        return await _context.Reviews
            .Include(r => r.Reviewer)
            .Include(r => r.Booking)
                .ThenInclude(b => b.PetCareRequest)
                    .ThenInclude(pcr => pcr.Pet)
            .Where(r => r.RevieweeId == userId && r.IsVisible)
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<bool> ValidateReviewAsync(Review review)
    {
        // Basic validation
        if (review.Rating < 1 || review.Rating > 5)
            return false;

        if (string.IsNullOrWhiteSpace(review.ReviewerId) || string.IsNullOrWhiteSpace(review.RevieweeId))
            return false;

        if (review.ReviewerId == review.RevieweeId)
            return false;

        // Check if booking exists and is completed
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == review.BookingId);

        if (booking == null || booking.Status != BookingStatus.Completed)
            return false;

        // Validate review type based on booking participants
        if (review.ReviewType == ReviewType.OwnerToSitter)
        {
            return booking.OwnerId == review.ReviewerId && booking.SitterId == review.RevieweeId;
        }
        else if (review.ReviewType == ReviewType.SitterToOwner)
        {
            return booking.SitterId == review.ReviewerId && booking.OwnerId == review.RevieweeId;
        }

        return false;
    }

    public async Task<IEnumerable<Review>> GetPendingReviewsAsync(string userId)
    {
        // Get completed bookings where user hasn't left a review yet
        var completedBookings = await _context.Bookings
            .Include(b => b.PetCareRequest)
                .ThenInclude(pcr => pcr.Pet)
            .Include(b => b.Owner)
            .Include(b => b.Sitter)
            .Where(b => b.Status == BookingStatus.Completed && 
                       (b.OwnerId == userId || b.SitterId == userId))
            .ToListAsync();

        var pendingReviews = new List<Review>();

        foreach (var booking in completedBookings)
        {
            // Check if user has already reviewed this booking
            var hasReviewed = await HasUserReviewedBookingAsync(booking.Id, userId);
            
            if (!hasReviewed)
            {
                // Create a placeholder review object for UI purposes
                var reviewType = booking.OwnerId == userId ? ReviewType.OwnerToSitter : ReviewType.SitterToOwner;
                var revieweeId = booking.OwnerId == userId ? booking.SitterId : booking.OwnerId;
                var reviewee = booking.OwnerId == userId ? booking.Sitter : booking.Owner;

                pendingReviews.Add(new Review
                {
                    BookingId = booking.Id,
                    ReviewerId = userId,
                    RevieweeId = revieweeId,
                    ReviewType = reviewType,
                    Booking = booking,
                    Reviewee = reviewee
                });
            }
        }

        return pendingReviews;
    }
}