using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using PetSitterConnect.Interfaces;
using PetSitterConnect.Models;

namespace PetSitterConnect.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public async Task JoinBookingChat(int bookingId)
    {
        var groupName = $"booking_{bookingId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("User {UserId} joined booking chat {BookingId}", Context.UserIdentifier, bookingId);
    }

    public async Task LeaveBookingChat(int bookingId)
    {
        var groupName = $"booking_{bookingId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("User {UserId} left booking chat {BookingId}", Context.UserIdentifier, bookingId);
    }

    public async Task SendMessage(int bookingId, string message)
    {
        try
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized message attempt for booking {BookingId}", bookingId);
                return;
            }

            // Create and save the message
            var chatMessage = new ChatMessage
            {
                BookingId = bookingId,
                SenderId = userId,
                Content = message,
                SentAt = DateTime.UtcNow,
                MessageType = MessageType.Text
            };

            var savedMessage = await _chatService.SendMessageAsync(chatMessage);

            // Send to all users in the booking chat group
            var groupName = $"booking_{bookingId}";
            await Clients.Group(groupName).SendAsync("ReceiveMessage", new
            {
                Id = savedMessage.Id,
                BookingId = savedMessage.BookingId,
                SenderId = savedMessage.SenderId,
                SenderName = savedMessage.Sender?.FirstName + " " + savedMessage.Sender?.LastName,
                Content = savedMessage.Content,
                SentAt = savedMessage.SentAt,
                MessageType = savedMessage.MessageType.ToString()
            });

            _logger.LogInformation("Message sent in booking {BookingId} by user {UserId}", bookingId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message for booking {BookingId}", bookingId);
            await Clients.Caller.SendAsync("MessageError", "Failed to send message");
        }
    }

    public async Task SendTypingIndicator(int bookingId, bool isTyping)
    {
        try
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId))
                return;

            var groupName = $"booking_{bookingId}";
            await Clients.OthersInGroup(groupName).SendAsync("UserTyping", new
            {
                UserId = userId,
                BookingId = bookingId,
                IsTyping = isTyping
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending typing indicator for booking {BookingId}", bookingId);
        }
    }

    public async Task MarkMessageAsRead(int messageId)
    {
        try
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId))
                return;

            await _chatService.MarkMessageAsReadAsync(messageId, userId);
            
            // Notify sender that message was read
            var message = await _chatService.GetMessageByIdAsync(messageId);
            if (message != null)
            {
                await Clients.User(message.SenderId).SendAsync("MessageRead", new
                {
                    MessageId = messageId,
                    ReadBy = userId,
                    ReadAt = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message {MessageId} as read", messageId);
        }
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("User {UserId} connected to chat hub", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("User {UserId} disconnected from chat hub", userId);
        
        if (exception != null)
        {
            _logger.LogError(exception, "User {UserId} disconnected with error", userId);
        }
        
        await base.OnDisconnectedAsync(exception);
    }
}