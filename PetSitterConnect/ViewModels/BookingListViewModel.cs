using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetSitterConnect.Models;
using PetSitterConnect.Services;
using System.Collections.ObjectModel;

namespace PetSitterConnect.ViewModels;

public partial class BookingListViewModel : BaseViewModel
{
    private readonly IBookingService _bookingService;
    private readonly IAuthService _authService;

    public BookingListViewModel(
        IBookingService bookingService,
        IAuthService authService)
    {
        _bookingService = bookingService;
        _authService = authService;
        Title = "My Bookings";
        
        Bookings = new ObservableCollection<Booking>();
        FilteredBookings = new ObservableCollection<Booking>();
        SelectedFilter = BookingFilter.All;
    }

    [ObservableProperty]
    private ObservableCollection<Booking> bookings;

    [ObservableProperty]
    private ObservableCollection<Booking> filteredBookings;

    [ObservableProperty]
    private BookingFilter selectedFilter;

    [ObservableProperty]
    private User? currentUser;

    [ObservableProperty]
    private bool showAsSitter;

    [ObservableProperty]
    private Booking? selectedBooking;

    [ObservableProperty]
    private string userRoleIcon = "👤";

    [ObservableProperty]
    private string userRoleLabel = "USER";

    [ObservableProperty]
    private Color userRoleColor = Colors.Gray;

    public List<BookingFilter> FilterOptions { get; } = new()
    {
        BookingFilter.All,
        BookingFilter.Pending,
        BookingFilter.Confirmed,
        BookingFilter.InProgress,
        BookingFilter.Completed,
        BookingFilter.Cancelled
    };

    public override async Task InitializeAsync()
    {
        CurrentUser = await _authService.GetCurrentUserAsync();
        if (CurrentUser != null)
        {
            UpdateUserRoleDisplay();

            // Set default view mode based on user type
            if (CurrentUser.UserType == UserType.PetSitter)
            {
                ShowAsSitter = true;
                Title = "Bookings as Sitter";
            }
            else if (CurrentUser.UserType == UserType.PetOwner)
            {
                ShowAsSitter = false;
                Title = "Bookings as Owner";
            }
            else // UserType.Both
            {
                ShowAsSitter = false; // Default to owner view for users with both roles
                Title = "Bookings as Owner";
            }
        }
        await LoadBookingsAsync();
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
    private async Task LoadBookingsAsync()
    {
        await ExecuteAsync(async () =>
        {
            if (CurrentUser == null) return;

            IEnumerable<Booking> bookings;

            if (ShowAsSitter)
            {
                bookings = await _bookingService.GetBookingsBySitterAsync(CurrentUser.Id);
            }
            else
            {
                bookings = await _bookingService.GetBookingsByOwnerAsync(CurrentUser.Id);
            }

            Bookings.Clear();
            foreach (var booking in bookings.OrderByDescending(b => b.CreatedAt))
            {
                Bookings.Add(booking);
            }

            ApplyFilter();
        });
    }

    [RelayCommand]
    private void ApplyFilter()
    {
        var filtered = Bookings.AsEnumerable();

        switch (SelectedFilter)
        {
            case BookingFilter.Pending:
                filtered = filtered.Where(b => b.Status == BookingStatus.Pending);
                break;
            case BookingFilter.Confirmed:
                filtered = filtered.Where(b => b.Status == BookingStatus.Confirmed);
                break;
            case BookingFilter.InProgress:
                filtered = filtered.Where(b => b.Status == BookingStatus.InProgress);
                break;
            case BookingFilter.Completed:
                filtered = filtered.Where(b => b.Status == BookingStatus.Completed);
                break;
            case BookingFilter.Cancelled:
                filtered = filtered.Where(b => b.Status == BookingStatus.Cancelled || b.Status == BookingStatus.Rejected);
                break;
            case BookingFilter.All:
            default:
                // No filtering
                break;
        }

        FilteredBookings.Clear();
        foreach (var booking in filtered)
        {
            FilteredBookings.Add(booking);
        }
    }

    [RelayCommand]
    private async Task ViewBookingDetailsAsync(Booking booking)
    {
        if (booking == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "BookingId", booking.Id }
        };

        await Shell.Current.GoToAsync("bookingdetails", navigationParameter);
    }

    [RelayCommand]
    private async Task ToggleViewModeAsync()
    {
        ShowAsSitter = !ShowAsSitter;
        Title = ShowAsSitter ? "Bookings as Sitter" : "Bookings as Owner";
        await LoadBookingsAsync();
    }

    [RelayCommand]
    private async Task AcceptBookingAsync(Booking booking)
    {
        if (booking == null || booking.Status != BookingStatus.Pending) return;

        await ExecuteAsync(async () =>
        {
            var success = await _bookingService.AcceptBookingAsync(booking.Id);
            if (success)
            {
                booking.Status = BookingStatus.Confirmed;
                booking.AcceptedAt = DateTime.UtcNow;
                await Shell.Current.DisplayAlert("Success", "Booking accepted successfully!", "OK");
                ApplyFilter();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to accept booking", "OK");
            }
        });
    }

    [RelayCommand]
    private async Task RejectBookingAsync(Booking booking)
    {
        if (booking == null || booking.Status != BookingStatus.Pending) return;

        var reason = await Shell.Current.DisplayPromptAsync("Reject Booking", 
            "Please provide a reason for rejection:", "Reject", "Cancel");

        if (string.IsNullOrWhiteSpace(reason)) return;

        await ExecuteAsync(async () =>
        {
            var success = await _bookingService.RejectBookingAsync(booking.Id, reason);
            if (success)
            {
                booking.Status = BookingStatus.Rejected;
                booking.CancellationReason = reason;
                await Shell.Current.DisplayAlert("Success", "Booking rejected", "OK");
                ApplyFilter();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to reject booking", "OK");
            }
        });
    }

    [RelayCommand]
    private async Task CancelBookingAsync(Booking booking)
    {
        if (booking == null || !booking.CanBeCancelled) return;

        var confirm = await Shell.Current.DisplayAlert("Cancel Booking", 
            "Are you sure you want to cancel this booking?", "Yes", "No");

        if (!confirm) return;

        var reason = await Shell.Current.DisplayPromptAsync("Cancel Booking", 
            "Please provide a reason for cancellation:", "Cancel", "Back");

        if (string.IsNullOrWhiteSpace(reason)) return;

        await ExecuteAsync(async () =>
        {
            var success = await _bookingService.CancelBookingAsync(booking.Id, reason);
            if (success)
            {
                booking.Status = BookingStatus.Cancelled;
                booking.CancellationReason = reason;
                await Shell.Current.DisplayAlert("Success", "Booking cancelled", "OK");
                ApplyFilter();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to cancel booking", "OK");
            }
        });
    }

    [RelayCommand]
    private async Task CompleteBookingAsync(Booking booking)
    {
        if (booking == null || !booking.CanBeCompleted) return;

        var confirm = await Shell.Current.DisplayAlert("Complete Booking", 
            "Mark this booking as completed?", "Yes", "No");

        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            var success = await _bookingService.CompleteBookingAsync(booking.Id);
            if (success)
            {
                booking.Status = BookingStatus.Completed;
                booking.CompletedAt = DateTime.UtcNow;
                await Shell.Current.DisplayAlert("Success", "Booking completed! You can now leave a review.", "OK");
                ApplyFilter();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to complete booking", "OK");
            }
        });
    }

    partial void OnSelectedFilterChanged(BookingFilter value)
    {
        ApplyFilter();
    }
}

public enum BookingFilter
{
    All,
    Pending,
    Confirmed,
    InProgress,
    Completed,
    Cancelled
}
