using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetSitterConnect.Interfaces;
using PetSitterConnect.Models;
using System.Collections.ObjectModel;

namespace PetSitterConnect.ViewModels;

[QueryProperty(nameof(BookingId), "BookingId")]
public partial class CreateReviewViewModel : BaseViewModel
{
    private readonly IReviewService _reviewService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private int bookingId;

    [ObservableProperty]
    private Booking? booking;

    [ObservableProperty]
    private User? reviewee;

    [ObservableProperty]
    private int selectedRating = 5;

    [ObservableProperty]
    private string comment = string.Empty;

    [ObservableProperty]
    private bool isSubmitting;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public ObservableCollection<int> RatingOptions { get; } = new() { 1, 2, 3, 4, 5 };

    public CreateReviewViewModel(IReviewService reviewService, IAuthService authService)
    {
        _reviewService = reviewService;
        _authService = authService;
        Title = "Write Review";
    }

    partial void OnBookingIdChanged(int value)
    {
        if (value > 0)
        {
            Task.Run(async () => await LoadBookingDetailsAsync());
        }
    }

    [RelayCommand]
    private async Task LoadBookingDetailsAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            // Get current user
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                ErrorMessage = "Please log in to continue";
                return;
            }

            // Check if user can review this booking
            if (!await _reviewService.CanUserReviewBookingAsync(BookingId, currentUser.Id))
            {
                ErrorMessage = "You cannot review this booking";
                return;
            }

            // Check if user has already reviewed
            if (await _reviewService.HasUserReviewedBookingAsync(BookingId, currentUser.Id))
            {
                ErrorMessage = "You have already reviewed this booking";
                return;
            }

            // Load booking details (this would need to be implemented in BookingService)
            // For now, we'll create a placeholder method
            await LoadBookingFromService();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading booking: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadBookingFromService()
    {
        // This is a placeholder - you'll need to implement GetBookingAsync in IBookingService
        // For now, we'll simulate loading
        await Task.Delay(500);
        
        // In a real implementation, you would:
        // Booking = await _bookingService.GetBookingAsync(BookingId);
        // Reviewee = Booking.OwnerId == currentUser.Id ? Booking.Sitter : Booking.Owner;
    }

    [RelayCommand]
    private async Task SubmitReviewAsync()
    {
        if (IsSubmitting) return;

        try
        {
            IsSubmitting = true;
            ErrorMessage = string.Empty;

            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                ErrorMessage = "Please log in to continue";
                return;
            }

            if (Booking == null || Reviewee == null)
            {
                ErrorMessage = "Booking information not available";
                return;
            }

            // Determine review type
            var reviewType = Booking.OwnerId == currentUser.Id 
                ? ReviewType.OwnerToSitter 
                : ReviewType.SitterToOwner;

            var review = new Review
            {
                BookingId = BookingId,
                ReviewerId = currentUser.Id,
                RevieweeId = Reviewee.Id,
                ReviewType = reviewType,
                Rating = SelectedRating,
                Comment = Comment?.Trim()
            };

            await _reviewService.CreateReviewAsync(review);

            // Navigate back with success message
            await Shell.Current.DisplayAlert("Success", "Review submitted successfully!", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error submitting review: {ex.Message}";
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private void SelectRating(int rating)
    {
        SelectedRating = rating;
    }
}