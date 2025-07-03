using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetSitterConnect.Models;
using PetSitterConnect.Services;
using System.Collections.ObjectModel;

namespace PetSitterConnect.ViewModels;

[QueryProperty(nameof(BookingId), "BookingId")]
public partial class ChatViewModel : BaseViewModel
{
    private readonly IChatService _chatService;
    private readonly IAuthService _authService;
    private readonly IBookingService _bookingService;

    public ChatViewModel(
        IChatService chatService, 
        IAuthService authService,
        IBookingService bookingService)
    {
        _chatService = chatService;
        _authService = authService;
        _bookingService = bookingService;
        Title = "Chat";
        
        Messages = new ObservableCollection<ChatMessage>();
    }

    [ObservableProperty]
    private int bookingId;

    [ObservableProperty]
    private ObservableCollection<ChatMessage> messages;

    [ObservableProperty]
    private string messageText = string.Empty;

    [ObservableProperty]
    private User? currentUser;

    [ObservableProperty]
    private Booking? booking;

    [ObservableProperty]
    private string otherParticipantName = string.Empty;

    [ObservableProperty]
    private string chatTitle = "Chat";

    [ObservableProperty]
    private bool canSendMessage;

    [ObservableProperty]
    private bool isTyping;

    [ObservableProperty]
    private string userRoleIcon = "👤";

    [ObservableProperty]
    private Color userRoleColor = Colors.Gray;

    public override async Task InitializeAsync()
    {
        await LoadChatDataAsync();
    }

    private async Task LoadChatDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            if (BookingId <= 0) return;

            CurrentUser = await _authService.GetCurrentUserAsync();
            if (CurrentUser == null) return;

            Booking = await _bookingService.GetBookingByIdAsync(BookingId);
            if (Booking == null) return;

            // Set up chat title and other participant info
            var isOwner = Booking.OwnerId == CurrentUser.Id;
            var otherParticipant = isOwner ? Booking.Sitter : Booking.Owner;
            OtherParticipantName = otherParticipant.FullName;
            ChatTitle = $"{otherParticipant.FirstName} - {Booking.PetCareRequest?.Pet?.Name}";

            // Update user role display
            UpdateUserRoleDisplay();

            // Check if user can send messages (booking must be active)
            CanSendMessage = Booking.IsActive;

            // Load messages
            await LoadMessagesAsync();

            // Mark all messages as read
            await _chatService.MarkAllMessagesAsReadAsync(BookingId, CurrentUser.Id);
        });
    }

    private void UpdateUserRoleDisplay()
    {
        if (CurrentUser == null) return;

        UserRoleIcon = CurrentUser.UserType switch
        {
            UserType.PetOwner => "🏠",
            UserType.PetSitter => "🐕‍🦺",
            UserType.Both => "🏠🐕‍🦺",
            _ => "👤"
        };

        UserRoleColor = CurrentUser.UserType switch
        {
            UserType.PetOwner => Color.FromArgb("#2E7D32"), // Green
            UserType.PetSitter => Color.FromArgb("#1976D2"), // Blue
            UserType.Both => Color.FromArgb("#7B1FA2"), // Purple
            _ => Colors.Gray
        };
    }

    [RelayCommand]
    private async Task LoadMessagesAsync()
    {
        await ExecuteAsync(async () =>
        {
            var messages = await _chatService.GetRecentMessagesAsync(BookingId);
            
            Messages.Clear();
            foreach (var message in messages)
            {
                Messages.Add(message);
            }
        });
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageText) || CurrentUser == null || !CanSendMessage || Booking == null)
            return;

        var messageContent = MessageText.Trim();
        MessageText = string.Empty; // Clear input immediately

        await ExecuteAsync(async () =>
        {
            // Determine the receiver (the other participant in the chat)
            var isOwner = Booking.OwnerId == CurrentUser.Id;
            var receiverId = isOwner ? Booking.SitterId : Booking.OwnerId;

            var message = new ChatMessage
            {
                BookingId = BookingId,
                SenderId = CurrentUser.Id,
                ReceiverId = receiverId,
                Content = messageContent,
                MessageType = MessageType.Text
            };

            var sentMessage = await _chatService.SendMessageAsync(message);
            Messages.Add(sentMessage);

            // Scroll to bottom (handled in UI)
        });
    }

    [RelayCommand]
    private async Task DeleteMessageAsync(ChatMessage message)
    {
        if (message == null || CurrentUser == null || message.SenderId != CurrentUser.Id)
            return;

        var confirm = await Shell.Current.DisplayAlert("Delete Message", 
            "Are you sure you want to delete this message?", "Delete", "Cancel");

        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            var success = await _chatService.DeleteMessageAsync(message.Id, CurrentUser.Id);
            if (success)
            {
                Messages.Remove(message);
            }
        });
    }

    [RelayCommand]
    private async Task ViewBookingDetailsAsync()
    {
        if (Booking == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "BookingId", Booking.Id }
        };

        await Shell.Current.GoToAsync("bookingdetails", navigationParameter);
    }

    [RelayCommand]
    private async Task RefreshMessagesAsync()
    {
        await LoadMessagesAsync();
    }

    public bool IsMyMessage(ChatMessage message)
    {
        return CurrentUser != null && message.SenderId == CurrentUser.Id;
    }

    public string GetMessageTimeDisplay(ChatMessage message)
    {
        var messageTime = message.SentAt;
        var now = DateTime.UtcNow;
        var diff = now - messageTime;

        if (diff.TotalMinutes < 1)
            return "Just now";
        else if (diff.TotalHours < 1)
            return $"{(int)diff.TotalMinutes}m ago";
        else if (diff.TotalDays < 1)
            return messageTime.ToString("HH:mm");
        else if (diff.TotalDays < 7)
            return messageTime.ToString("ddd HH:mm");
        else
            return messageTime.ToString("MMM dd HH:mm");
    }

    public string GetSenderName(ChatMessage message)
    {
        if (IsMyMessage(message))
            return "You";
        
        return message.Sender?.FirstName ?? "Unknown";
    }

    partial void OnBookingIdChanged(int value)
    {
        if (value > 0)
        {
            Task.Run(async () => await LoadChatDataAsync());
        }
    }
}
