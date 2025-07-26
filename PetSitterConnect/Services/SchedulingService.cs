using Microsoft.EntityFrameworkCore;
using PetSitterConnect.Data;
using PetSitterConnect.Models;
using PetSitterConnect.Interfaces;

namespace PetSitterConnect.Services;

public class SchedulingService : ISchedulingService
{
    private readonly PetSitterDbContext _context;

    public SchedulingService(PetSitterDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsAvailableAsync(string sitterId, DateTime startDate, DateTime endDate)
    {
        var conflictingBookings = await GetConflictingBookingsAsync(sitterId, startDate, endDate);
        return !conflictingBookings.Any();
    }

    public async Task<IEnumerable<Booking>> GetConflictingBookingsAsync(string sitterId, DateTime startDate, DateTime endDate)
    {
        return await _context.Bookings
            .Include(b => b.PetCareRequest)
            .Where(b => b.SitterId == sitterId &&
                       (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress) &&
                       b.PetCareRequest.StartDate < endDate &&
                       b.PetCareRequest.EndDate > startDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetUpcomingBookingsAsync(string userId)
    {
        var today = DateTime.Today;
        
        return await _context.Bookings
            .Include(b => b.PetCareRequest)
                .ThenInclude(r => r.Pet)
            .Include(b => b.Sitter)
            .Include(b => b.Owner)
            .Where(b => (b.SitterId == userId || b.OwnerId == userId) &&
                       b.PetCareRequest.StartDate >= today &&
                       (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress))
            .OrderBy(b => b.PetCareRequest.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetBookingsForDateAsync(string userId, DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await _context.Bookings
            .Include(b => b.PetCareRequest)
                .ThenInclude(r => r.Pet)
            .Include(b => b.Sitter)
            .Include(b => b.Owner)
            .Where(b => (b.SitterId == userId || b.OwnerId == userId) &&
                       b.PetCareRequest.StartDate < endOfDay &&
                       b.PetCareRequest.EndDate >= startOfDay &&
                       (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress))
            .ToListAsync();
    }

    public async Task<bool> HasConflictAsync(string sitterId, DateTime startDate, DateTime endDate, int? excludeBookingId = null)
    {
        var query = _context.Bookings
            .Include(b => b.PetCareRequest)
            .Where(b => b.SitterId == sitterId &&
                       (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress) &&
                       b.PetCareRequest.StartDate < endDate &&
                       b.PetCareRequest.EndDate > startDate);

        if (excludeBookingId.HasValue)
        {
            query = query.Where(b => b.Id != excludeBookingId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<Dictionary<DateTime, int>> GetBookingCountsByDateAsync(string sitterId, DateTime startDate, DateTime endDate)
    {
        var bookings = await _context.Bookings
            .Include(b => b.PetCareRequest)
            .Where(b => b.SitterId == sitterId &&
                       (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress) &&
                       b.PetCareRequest.StartDate < endDate &&
                       b.PetCareRequest.EndDate > startDate)
            .ToListAsync();

        var bookingCounts = new Dictionary<DateTime, int>();
        var currentDate = startDate.Date;

        while (currentDate <= endDate.Date)
        {
            var count = bookings.Count(b => 
                b.PetCareRequest.StartDate.Date <= currentDate && 
                b.PetCareRequest.EndDate.Date >= currentDate);
            
            bookingCounts[currentDate] = count;
            currentDate = currentDate.AddDays(1);
        }

        return bookingCounts;
    }

    public async Task<bool> CanAcceptBookingAsync(string sitterId, int bookingId)
    {
        var booking = await _context.Bookings
            .Include(b => b.PetCareRequest)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null || booking.SitterId != sitterId || booking.Status != BookingStatus.Pending)
        {
            return false;
        }

        // Check for scheduling conflicts
        var hasConflict = await HasConflictAsync(sitterId, 
            booking.PetCareRequest.StartDate, 
            booking.PetCareRequest.EndDate, 
            bookingId);

        return !hasConflict;
    }

    public async Task<IEnumerable<DateTime>> GetAvailableDatesAsync(string sitterId, DateTime startDate, DateTime endDate)
    {
        var bookingCounts = await GetBookingCountsByDateAsync(sitterId, startDate, endDate);
        
        // For simplicity, assume a sitter can only handle one booking at a time
        // This can be made configurable based on sitter preferences
        const int maxBookingsPerDay = 1;

        return bookingCounts
            .Where(kvp => kvp.Value < maxBookingsPerDay)
            .Select(kvp => kvp.Key)
            .ToList();
    }
}
