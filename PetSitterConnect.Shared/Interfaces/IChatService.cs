using PetSitterConnect.Models;

namespace PetSitterConnect.Interfaces;

public interface IChatService
{
    Task<IEnumerable<ChatMessage>> GetMessagesAsync(int bookingId);
    Task<ChatMessage> SendMessageAsync(ChatMessage message);
    Task<bool> MarkMessageAsReadAsync(int messageId);
    Task<bool> MarkMessageAsReadAsync(int messageId, string userId);
    Task<bool> MarkAllMessagesAsReadAsync(int bookingId, string userId);
    Task<int> GetUnreadMessageCountAsync(int bookingId, string userId);
    Task<IEnumerable<ChatConversation>> GetConversationsAsync(string userId);
    Task<ChatConversation?> GetConversationAsync(int bookingId);
    Task<bool> DeleteMessageAsync(int messageId, string userId);
    Task<IEnumerable<ChatMessage>> GetRecentMessagesAsync(int bookingId, int count = 50);
    Task<ChatMessage?> GetMessageByIdAsync(int messageId);
}

public class ChatConversation
{
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public ChatMessage? LastMessage { get; set; }
    public int UnreadCount { get; set; }
    public DateTime LastActivity { get; set; }
    public string OtherParticipantName { get; set; } = string.Empty;
    public string OtherParticipantId { get; set; } = string.Empty;
}
