using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetSitterConnect.Models;
using PetSitterConnect.Services;

namespace PetSitterConnect.ViewModels;

[QueryProperty(nameof(BookingId), "BookingId")]
public partial class BookingDetailViewModel : BaseViewModel
{
    private readonly IBookingService _bookingService;
    private readonly IAuthService _authService;

    public BookingDetailViewModel(
        IBookingService bookingService,
        IAuthService authService)
    {
        _bookingService = bookingService;
        _authService = authService;
        Title = "Booking Details";
    }

    [ObservableProperty]
    private int bookingId;

    [ObservableProperty]
    private Booking? booking;

    [ObservableProperty]
    private User? currentUser;

    [ObservableProperty]
    private bool isOwner;

    [ObservableProperty]
    private bool isSitter;

    [ObservableProperty]
    private bool canAccept;

    [ObservableProperty]
    private bool canReject;

    [ObservableProperty]
    private bool canCancel;

    [ObservableProperty]
    private bool canComplete;

    [ObservableProperty]
    private bool canMessage;

    [ObservableProperty]
    private bool canReview;

    [ObservableProperty]
    private string statusColor = "Gray";

    [ObservableProperty]
    private string statusText = string.Empty;

    [ObservableProperty]
    private User? contactUser;

    [ObservableProperty]
    private string contactUserRole = string.Empty;

    public override async Task InitializeAsync()
    {
        await LoadBookingDetailsAsync();
    }

    [RelayCommand]
    private async Task LoadBookingDetailsAsync()
    {
        await ExecuteAsync(async () =>
        {
            if (BookingId <= 0) return;

            CurrentUser = await _authService.GetCurrentUserAsync();
            Booking = await _bookingService.GetBookingByIdAsync(BookingId);

            if (Booking != null && CurrentUser != null)
            {
                IsOwner = Booking.OwnerId == CurrentUser.Id;
                IsSitter = Booking.SitterId == CurrentUser.Id;

                UpdateContactInformation();
                UpdateActionButtons();
                UpdateStatusDisplay();
            }
        });
    }

    private void UpdateContactInformation()
    {
        if (Booking == null || CurrentUser == null) return;

        // Show the other party's information
        ContactUser = IsOwner ? Booking.Sitter : Booking.Owner;
        ContactUserRole = IsOwner ? "Pet Sitter" : "Pet Owner";
    }

    private void UpdateActionButtons()
    {
        if (Booking == null || CurrentUser == null) return;

        // Sitter actions
        CanAccept = IsSitter && Booking.Status == BookingStatus.Pending;
        CanReject = IsSitter && Booking.Status == BookingStatus.Pending;

        // Both parties can cancel (with different conditions)
        CanCancel = Booking.CanBeCancelled && (IsOwner || IsSitter);

        // Sitter can complete when service period ends
        CanComplete = IsSitter && Booking.CanBeCompleted;

        // Both parties can message during active booking
        CanMessage = Booking.IsActive;

        // Both parties can review after completion
        CanReview = Booking.Status == BookingStatus.Completed;
    }

    private void UpdateStatusDisplay()
    {
        if (Booking == null) return;

        StatusText = Booking.Status.ToString();
        StatusColor = Booking.Status switch
        {
            BookingStatus.Pending => "Orange",
            BookingStatus.Confirmed => "Blue",
            BookingStatus.InProgress => "Green",
            BookingStatus.Completed => "DarkGreen",
            BookingStatus.Cancelled => "Red",
            BookingStatus.Rejected => "DarkRed",
            _ => "Gray"
        };
    }

    [RelayCommand]
    private async Task AcceptBookingAsync()
    {
        if (Booking == null || !CanAccept) return;

        await ExecuteAsync(async () =>
        {
            var success = await _bookingService.AcceptBookingAsync(Booking.Id);
            if (success)
            {
                await Shell.Current.DisplayAlert("Success", "Booking accepted successfully!", "OK");
                await LoadBookingDetailsAsync();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to accept booking", "OK");
            }
        });
    }

    [RelayCommand]
    private async Task RejectBookingAsync()
    {
        if (Booking == null || !CanReject) return;

        var reason = await Shell.Current.DisplayPromptAsync("Reject Booking", 
            "Please provide a reason for rejection:", "Reject", "Cancel");

        if (string.IsNullOrWhiteSpace(reason)) return;

        await ExecuteAsync(async () =>
        {
            var success = await _bookingService.RejectBookingAsync(Booking.Id, reason);
            if (success)
            {
                await Shell.Current.DisplayAlert("Success", "Booking rejected", "OK");
                await LoadBookingDetailsAsync();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to reject booking", "OK");
            }
        });
    }

    [RelayCommand]
    private async Task CancelBookingAsync()
    {
        if (Booking == null || !CanCancel) return;

        var confirm = await Shell.Current.DisplayAlert("Cancel Booking", 
            "Are you sure you want to cancel this booking?", "Yes", "No");

        if (!confirm) return;

        var reason = await Shell.Current.DisplayPromptAsync("Cancel Booking", 
            "Please provide a reason for cancellation:", "Cancel", "Back");

        if (string.IsNullOrWhiteSpace(reason)) return;

        await ExecuteAsync(async () =>
        {
            var success = await _bookingService.CancelBookingAsync(Booking.Id, reason);
            if (success)
            {
                await Shell.Current.DisplayAlert("Success", "Booking cancelled", "OK");
                await LoadBookingDetailsAsync();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to cancel booking", "OK");
            }
        });
    }

    [RelayCommand]
    private async Task CompleteBookingAsync()
    {
        if (Booking == null || !CanComplete) return;

        var confirm = await Shell.Current.DisplayAlert("Complete Booking", 
            "Mark this booking as completed?", "Yes", "No");

        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            var success = await _bookingService.CompleteBookingAsync(Booking.Id);
            if (success)
            {
                await Shell.Current.DisplayAlert("Success", "Booking completed! You can now leave a review.", "OK");
                await LoadBookingDetailsAsync();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to complete booking", "OK");
            }
        });
    }

    [RelayCommand]
    private async Task OpenChatAsync()
    {
        if (Booking == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "BookingId", Booking.Id }
        };

        await Shell.Current.GoToAsync("chat", navigationParameter);
    }

    [RelayCommand]
    private async Task LeaveReviewAsync()
    {
        if (Booking == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "BookingId", Booking.Id }
        };

        await Shell.Current.GoToAsync("leavereview", navigationParameter);
    }

    [RelayCommand]
    private async Task ViewPetDetailsAsync()
    {
        if (Booking?.PetCareRequest?.Pet == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "PetId", Booking.PetCareRequest.Pet.Id }
        };

        await Shell.Current.GoToAsync("petdetails", navigationParameter);
    }

    [RelayCommand]
    private async Task ContactUserAsync()
    {
        if (Booking == null) return;

        var contactUser = IsOwner ? Booking.Sitter : Booking.Owner;
        var contactType = IsOwner ? "sitter" : "pet owner";

        await Shell.Current.DisplayAlert("Contact Information", 
            $"Contact {contactUser.FullName} ({contactType}) at {contactUser.Email}", "OK");
    }

    partial void OnBookingIdChanged(int value)
    {
        if (value > 0)
        {
            Task.Run(async () => await LoadBookingDetailsAsync());
        }
    }
}
