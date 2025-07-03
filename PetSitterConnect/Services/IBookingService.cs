using PetSitterConnect.Models;

namespace PetSitterConnect.Services;

public interface IBookingService
{
    Task<Booking?> GetBookingByIdAsync(int bookingId);
    Task<Booking?> GetBookingByRequestAndSitterAsync(int requestId, string sitterId);
    Task<IEnumerable<Booking>> GetBookingsBySitterAsync(string sitterId);
    Task<IEnumerable<Booking>> GetBookingsByOwnerAsync(string ownerId);
    Task<Booking> CreateBookingAsync(Booking booking);
    Task<bool> UpdateBookingAsync(Booking booking);
    Task<bool> UpdateBookingStatusAsync(int bookingId, BookingStatus status);
    Task<bool> AcceptBookingAsync(int bookingId);
    Task<bool> RejectBookingAsync(int bookingId, string reason);
    Task<bool> CancelBookingAsync(int bookingId, string reason);
    Task<bool> CompleteBookingAsync(int bookingId);
    Task<IEnumerable<Booking>> GetActiveBookingsAsync(string userId);
    Task<IEnumerable<Booking>> GetBookingHistoryAsync(string userId);
}
