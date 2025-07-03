using Microsoft.EntityFrameworkCore;
using PetSitterConnect.Data;
using PetSitterConnect.Models;

namespace PetSitterConnect.Services;

public class BookingService : IBookingService
{
    private readonly PetSitterDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ISchedulingService _schedulingService;

    public BookingService(
        PetSitterDbContext context,
        INotificationService notificationService,
        ISchedulingService schedulingService)
    {
        _context = context;
        _notificationService = notificationService;
        _schedulingService = schedulingService;
    }

    public async Task<Booking?> GetBookingByIdAsync(int bookingId)
    {
        return await _context.Bookings
            .Include(b => b.PetCareRequest)
                .ThenInclude(r => r.Pet)
            .Include(b => b.Sitter)
            .Include(b => b.Owner)
            .Include(b => b.Reviews)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
    }

    public async Task<Booking?> GetBookingByRequestAndSitterAsync(int requestId, string sitterId)
    {
        return await _context.Bookings
            .Include(b => b.PetCareRequest)
                .ThenInclude(r => r.Pet)
            .Include(b => b.Sitter)
            .Include(b => b.Owner)
            .FirstOrDefaultAsync(b => b.PetCareRequestId == requestId && b.SitterId == sitterId);
    }

    public async Task<IEnumerable<Booking>> GetBookingsBySitterAsync(string sitterId)
    {
        return await _context.Bookings
            .Include(b => b.PetCareRequest)
                .ThenInclude(r => r.Pet)
            .Include(b => b.Owner)
            .Where(b => b.SitterId == sitterId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetBookingsByOwnerAsync(string ownerId)
    {
        return await _context.Bookings
            .Include(b => b.PetCareRequest)
                .ThenInclude(r => r.Pet)
            .Include(b => b.Sitter)
            .Where(b => b.OwnerId == ownerId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<Booking> CreateBookingAsync(Booking booking)
    {
        booking.CreatedAt = DateTime.UtcNow;
        booking.UpdatedAt = DateTime.UtcNow;
        booking.Status = BookingStatus.Pending;

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        // Load related data for notification
        await _context.Entry(booking)
            .Reference(b => b.PetCareRequest)
            .LoadAsync();
        await _context.Entry(booking.PetCareRequest)
            .Reference(r => r.Pet)
            .LoadAsync();
        await _context.Entry(booking)
            .Reference(b => b.Sitter)
            .LoadAsync();

        // Send notification to pet owner
        await _notificationService.SendBookingApplicationNotificationAsync(booking);

        return booking;
    }

    public async Task<bool> UpdateBookingAsync(Booking booking)
    {
        try
        {
            booking.UpdatedAt = DateTime.UtcNow;
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateBookingStatusAsync(int bookingId, BookingStatus status)
    {
        try
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return false;

            booking.Status = status;
            booking.UpdatedAt = DateTime.UtcNow;

            switch (status)
            {
                case BookingStatus.Confirmed:
                    booking.AcceptedAt = DateTime.UtcNow;
                    break;
                case BookingStatus.Completed:
                    booking.CompletedAt = DateTime.UtcNow;
                    break;
                case BookingStatus.Cancelled:
                    booking.CancelledAt = DateTime.UtcNow;
                    break;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> AcceptBookingAsync(int bookingId)
    {
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.PetCareRequest)
                    .ThenInclude(r => r.Pet)
                .Include(b => b.Sitter)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null || booking.Status != BookingStatus.Pending)
                return false;

            // Check for scheduling conflicts
            var canAccept = await _schedulingService.CanAcceptBookingAsync(booking.SitterId, bookingId);
            if (!canAccept)
                return false;

            booking.Status = BookingStatus.Confirmed;
            booking.AcceptedAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            // Update the pet care request status
            booking.PetCareRequest.Status = RequestStatus.InProgress;
            booking.PetCareRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send notification to pet owner
            await _notificationService.SendBookingAcceptedNotificationAsync(booking);

            // Schedule reminder notification
            var reminderTime = booking.PetCareRequest.StartDate.AddDays(-1);
            if (reminderTime > DateTime.UtcNow)
            {
                await _notificationService.ScheduleNotificationAsync(
                    "Booking Reminder",
                    $"You have a booking for {booking.PetCareRequest.Pet.Name} starting tomorrow.",
                    reminderTime);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RejectBookingAsync(int bookingId, string reason)
    {
        try
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null || booking.Status != BookingStatus.Pending)
                return false;

            booking.Status = BookingStatus.Rejected;
            booking.CancellationReason = reason;
            booking.CancelledAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CancelBookingAsync(int bookingId, string reason)
    {
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.PetCareRequest)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null || !booking.CanBeCancelled)
                return false;

            booking.Status = BookingStatus.Cancelled;
            booking.CancellationReason = reason;
            booking.CancelledAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            // Update the pet care request status back to open
            booking.PetCareRequest.Status = RequestStatus.Open;
            booking.PetCareRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CompleteBookingAsync(int bookingId)
    {
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.PetCareRequest)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null || !booking.CanBeCompleted)
                return false;

            booking.Status = BookingStatus.Completed;
            booking.CompletedAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            // Update the pet care request status
            booking.PetCareRequest.Status = RequestStatus.Completed;
            booking.PetCareRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<Booking>> GetActiveBookingsAsync(string userId)
    {
        return await _context.Bookings
            .Include(b => b.PetCareRequest)
                .ThenInclude(r => r.Pet)
            .Include(b => b.Sitter)
            .Include(b => b.Owner)
            .Where(b => (b.SitterId == userId || b.OwnerId == userId) && b.IsActive)
            .OrderBy(b => b.PetCareRequest.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetBookingHistoryAsync(string userId)
    {
        return await _context.Bookings
            .Include(b => b.PetCareRequest)
                .ThenInclude(r => r.Pet)
            .Include(b => b.Sitter)
            .Include(b => b.Owner)
            .Include(b => b.Reviews)
            .Where(b => (b.SitterId == userId || b.OwnerId == userId) 
                       && (b.Status == BookingStatus.Completed || b.Status == BookingStatus.Cancelled))
            .OrderByDescending(b => b.UpdatedAt)
            .ToListAsync();
    }
}
