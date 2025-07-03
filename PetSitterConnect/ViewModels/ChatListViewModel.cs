using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetSitterConnect.Models;
using PetSitterConnect.Services;
using System.Collections.ObjectModel;

namespace PetSitterConnect.ViewModels;

public partial class ChatListViewModel : BaseViewModel
{
    private readonly IChatService _chatService;
    private readonly IAuthService _authService;

    public ChatListViewModel(IChatService chatService, IAuthService authService)
    {
        _chatService = chatService;
        _authService = authService;
        Title = "Messages";
        
        Conversations = new ObservableCollection<ChatConversation>();
    }

    [ObservableProperty]
    private ObservableCollection<ChatConversation> conversations;

    [ObservableProperty]
    private User? currentUser;

    [ObservableProperty]
    private ChatConversation? selectedConversation;

    [ObservableProperty]
    private bool hasConversations;

    [ObservableProperty]
    private string userRoleIcon = "👤";

    [ObservableProperty]
    private string userRoleLabel = "USER";

    [ObservableProperty]
    private Color userRoleColor = Colors.Gray;

    public override async Task InitializeAsync()
    {
        await LoadUserInfoAsync();
        await LoadConversationsAsync();
    }

    private async Task LoadUserInfoAsync()
    {
        CurrentUser = await _authService.GetCurrentUserAsync();
        if (CurrentUser != null)
        {
            UpdateUserRoleDisplay();
        }
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

        UserRoleLabel = CurrentUser.UserType switch
        {
            UserType.PetOwner => "PET OWNER",
            UserType.PetSitter => "PET SITTER",
            UserType.Both => "OWNER & SITTER",
            _ => "USER"
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
    private async Task LoadConversationsAsync()
    {
        await ExecuteAsync(async () =>
        {
            if (CurrentUser == null) return;

            var conversations = await _chatService.GetConversationsAsync(CurrentUser.Id);
            
            Conversations.Clear();
            foreach (var conversation in conversations)
            {
                Conversations.Add(conversation);
            }

            HasConversations = Conversations.Any();
        });
    }

    [RelayCommand]
    private async Task OpenChatAsync(ChatConversation conversation)
    {
        if (conversation == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "BookingId", conversation.BookingId }
        };

        await Shell.Current.GoToAsync("chat", navigationParameter);
    }

    [RelayCommand]
    private async Task RefreshConversationsAsync()
    {
        await LoadConversationsAsync();
    }

    [RelayCommand]
    private async Task ViewBookingDetailsAsync(ChatConversation conversation)
    {
        if (conversation == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "BookingId", conversation.BookingId }
        };

        await Shell.Current.GoToAsync("bookingdetails", navigationParameter);
    }

    public string GetConversationTitle(ChatConversation conversation)
    {
        if (CurrentUser == null) return "Chat";

        var isOwner = conversation.Booking.OwnerId == CurrentUser.Id;
        var otherParty = isOwner ? conversation.Booking.Sitter : conversation.Booking.Owner;
        var petName = conversation.Booking.PetCareRequest?.Pet?.Name ?? "Pet";

        return $"{otherParty.FullName} - {petName}";
    }

    public string GetConversationSubtitle(ChatConversation conversation)
    {
        if (conversation.LastMessage != null)
        {
            var preview = conversation.LastMessage.Content.Length > 50 
                ? conversation.LastMessage.Content.Substring(0, 50) + "..."
                : conversation.LastMessage.Content;
            
            return $"{conversation.LastMessage.Sender.FirstName}: {preview}";
        }

        return "No messages yet";
    }

    public string GetTimeDisplay(ChatConversation conversation)
    {
        if (conversation.LastMessage == null) return "";

        var messageTime = conversation.LastMessage.SentAt;
        var now = DateTime.UtcNow;
        var diff = now - messageTime;

        if (diff.TotalMinutes < 1)
            return "Just now";
        else if (diff.TotalHours < 1)
            return $"{(int)diff.TotalMinutes}m ago";
        else if (diff.TotalDays < 1)
            return $"{(int)diff.TotalHours}h ago";
        else if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays}d ago";
        else
            return messageTime.ToString("MMM dd");
    }
}
