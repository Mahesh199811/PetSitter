using PetSitterConnect.Models;

namespace PetSitterConnect.Services;

public interface ISchedulingService
{
    Task<bool> IsAvailableAsync(string sitterId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<Booking>> GetConflictingBookingsAsync(string sitterId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<Booking>> GetUpcomingBookingsAsync(string userId);
    Task<IEnumerable<Booking>> GetBookingsForDateAsync(string userId, DateTime date);
    Task<bool> HasConflictAsync(string sitterId, DateTime startDate, DateTime endDate, int? excludeBookingId = null);
    Task<Dictionary<DateTime, int>> GetBookingCountsByDateAsync(string sitterId, DateTime startDate, DateTime endDate);
    Task<bool> CanAcceptBookingAsync(string sitterId, int bookingId);
    Task<IEnumerable<DateTime>> GetAvailableDatesAsync(string sitterId, DateTime startDate, DateTime endDate);
}
