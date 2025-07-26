using PetSitterConnect.Models;

namespace PetSitterConnect.Interfaces;

public interface INotificationService
{
    Task SendBookingApplicationNotificationAsync(Booking booking);
    Task SendBookingAcceptedNotificationAsync(Booking booking);
    Task SendBookingRejectedNotificationAsync(Booking booking);
    Task SendBookingCancelledNotificationAsync(Booking booking);
    Task SendBookingCompletedNotificationAsync(Booking booking);
    Task SendBookingReminderNotificationAsync(Booking booking);
    Task SendUpcomingBookingNotificationsAsync();
    Task<bool> ScheduleNotificationAsync(string title, string message, DateTime scheduledTime);
    Task<bool> CancelNotificationAsync(string notificationId);
}

public class NotificationData
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public string UserId { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

public enum NotificationType
{
    BookingApplication,
    BookingAccepted,
    BookingRejected,
    BookingCancelled,
    BookingCompleted,
    BookingReminder,
    MessageReceived,
    ReviewReceived
}
