using Microsoft.EntityFrameworkCore;
using PetSitterConnect.Data;
using PetSitterConnect.Models;

namespace PetSitterConnect.Services;

public class ChatService : IChatService
{
    private readonly PetSitterDbContext _context;

    public ChatService(PetSitterDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesAsync(int bookingId)
    {
        return await _context.ChatMessages
            .Include(m => m.Sender)
            .Where(m => m.BookingId == bookingId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();
    }

    public async Task<ChatMessage> SendMessageAsync(ChatMessage message)
    {
        message.SentAt = DateTime.UtcNow;
        message.IsRead = false;

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();

        // Load the sender information
        await _context.Entry(message)
            .Reference(m => m.Sender)
            .LoadAsync();

        return message;
    }

    public async Task<bool> MarkMessageAsReadAsync(int messageId)
    {
        var message = await _context.ChatMessages.FindAsync(messageId);
        if (message == null) return false;

        message.IsRead = true;
        message.ReadAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAllMessagesAsReadAsync(int bookingId, string userId)
    {
        var messages = await _context.ChatMessages
            .Where(m => m.BookingId == bookingId && 
                       m.SenderId != userId && 
                       !m.IsRead)
            .ToListAsync();

        foreach (var message in messages)
        {
            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetUnreadMessageCountAsync(int bookingId, string userId)
    {
        return await _context.ChatMessages
            .CountAsync(m => m.BookingId == bookingId && 
                           m.SenderId != userId && 
                           !m.IsRead);
    }

    public async Task<IEnumerable<ChatConversation>> GetConversationsAsync(string userId)
    {
        var bookings = await _context.Bookings
            .Include(b => b.PetCareRequest)
                .ThenInclude(r => r.Pet)
            .Include(b => b.Owner)
            .Include(b => b.Sitter)
            .Where(b => (b.OwnerId == userId || b.SitterId == userId) &&
                       (b.Status == BookingStatus.Confirmed || 
                        b.Status == BookingStatus.InProgress || 
                        b.Status == BookingStatus.Completed))
            .ToListAsync();

        var conversations = new List<ChatConversation>();

        foreach (var booking in bookings)
        {
            var lastMessage = await _context.ChatMessages
                .Where(m => m.BookingId == booking.Id)
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync();

            var unreadCount = await GetUnreadMessageCountAsync(booking.Id, userId);

            var otherParticipant = booking.OwnerId == userId ? booking.Sitter : booking.Owner;

            conversations.Add(new ChatConversation
            {
                BookingId = booking.Id,
                Booking = booking,
                LastMessage = lastMessage,
                UnreadCount = unreadCount,
                LastActivity = lastMessage?.SentAt ?? booking.CreatedAt,
                OtherParticipantName = otherParticipant.FullName,
                OtherParticipantId = otherParticipant.Id
            });
        }

        return conversations.OrderByDescending(c => c.LastActivity);
    }

    public async Task<ChatConversation?> GetConversationAsync(int bookingId)
    {
        var booking = await _context.Bookings
            .Include(b => b.PetCareRequest)
                .ThenInclude(r => r.Pet)
            .Include(b => b.Owner)
            .Include(b => b.Sitter)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null) return null;

        var lastMessage = await _context.ChatMessages
            .Where(m => m.BookingId == bookingId)
            .OrderByDescending(m => m.SentAt)
            .FirstOrDefaultAsync();

        return new ChatConversation
        {
            BookingId = booking.Id,
            Booking = booking,
            LastMessage = lastMessage,
            LastActivity = lastMessage?.SentAt ?? booking.CreatedAt
        };
    }

    public async Task<bool> DeleteMessageAsync(int messageId, string userId)
    {
        var message = await _context.ChatMessages.FindAsync(messageId);
        if (message == null || message.SenderId != userId) return false;

        // Soft delete - mark as deleted instead of removing
        message.IsDeleted = true;
        message.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ChatMessage>> GetRecentMessagesAsync(int bookingId, int count = 50)
    {
        return await _context.ChatMessages
            .Include(m => m.Sender)
            .Where(m => m.BookingId == bookingId && !m.IsDeleted)
            .OrderByDescending(m => m.SentAt)
            .Take(count)
            .OrderBy(m => m.SentAt)
            .ToListAsync();
    }
}
