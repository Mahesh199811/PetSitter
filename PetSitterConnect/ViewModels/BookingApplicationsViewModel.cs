using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetSitterConnect.Models;
using PetSitterConnect.Services;
using PetSitterConnect.Interfaces;
using System.Collections.ObjectModel;

namespace PetSitterConnect.ViewModels;

[QueryProperty(nameof(RequestId), "RequestId")]
public partial class BookingApplicationsViewModel : BaseViewModel
{
    private readonly IBookingService _bookingService;
    private readonly IPetCareRequestService _petCareRequestService;
    private readonly IAuthService _authService;

    public BookingApplicationsViewModel(
        IBookingService bookingService,
        IPetCareRequestService petCareRequestService,
        IAuthService authService)
    {
        _bookingService = bookingService;
        _petCareRequestService = petCareRequestService;
        _authService = authService;
        Title = "Applications";

        Applications = [];
    }

    [ObservableProperty]
    private int requestId;

    [ObservableProperty]
    private PetCareRequest? petCareRequest;

    [ObservableProperty]
    private ObservableCollection<Booking> applications;

    [ObservableProperty]
    private User? currentUser;

    [ObservableProperty]
    private bool hasApplications;

    [ObservableProperty]
    private Booking? selectedApplication;

    public override async Task InitializeAsync()
    {
        await LoadApplicationsAsync();
    }

    [RelayCommand]
    private async Task LoadApplicationsAsync()
    {
        await ExecuteAsync(async () =>
        {
            if (RequestId <= 0) return;

            CurrentUser = await _authService.GetCurrentUserAsync();
            PetCareRequest = await _petCareRequestService.GetRequestByIdAsync(RequestId);

            if (PetCareRequest == null || CurrentUser == null) return;

            // Verify the current user is the owner of this request
            if (PetCareRequest.OwnerId != CurrentUser.Id)
            {
                await Shell.Current.DisplayAlert("Error", "You can only view applications for your own requests", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            // Get all bookings for this request
            var allBookings = await _bookingService.GetBookingsByOwnerAsync(CurrentUser.Id);
            var requestBookings = allBookings.Where(b => b.PetCareRequestId == RequestId)
                                           .OrderByDescending(b => b.CreatedAt);

            Applications.Clear();
            foreach (var booking in requestBookings)
            {
                Applications.Add(booking);
            }

            HasApplications = Applications.Any();
            Title = $"Applications ({Applications.Count})";
        });
    }

    [RelayCommand]
    private async Task ViewSitterProfileAsync(Booking application)
    {
        if (application?.Sitter == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "UserId", application.SitterId }
        };

        await Shell.Current.GoToAsync("userprofile", navigationParameter);
    }

    [RelayCommand]
    private async Task AcceptApplicationAsync(Booking application)
    {
        if (application == null || application.Status != BookingStatus.Pending) return;

        var sitterName = application.Sitter?.FullName ?? "this sitter";
        var confirm = await Shell.Current.DisplayAlert("Accept Application", 
            $"Accept {sitterName}'s application? This will reject all other pending applications.", 
            "Accept", "Cancel");

        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            var success = await _bookingService.AcceptBookingAsync(application.Id);
            if (success)
            {
                // Reject all other pending applications for this request
                var otherPendingApps = Applications.Where(a => a.Id != application.Id && 
                                                              a.Status == BookingStatus.Pending);

                foreach (var otherApp in otherPendingApps)
                {
                    await _bookingService.RejectBookingAsync(otherApp.Id, 
                        "Another sitter was selected for this request");
                }

                await Shell.Current.DisplayAlert("Success", 
                    $"Application accepted! {sitterName} has been notified.", "OK");

                await LoadApplicationsAsync();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to accept application", "OK");
            }
        });
    }

    [RelayCommand]
    private async Task RejectApplicationAsync(Booking application)
    {
        if (application == null || application.Status != BookingStatus.Pending) return;

        var sitterName = application.Sitter?.FullName ?? "this sitter";
        var confirm = await Shell.Current.DisplayAlert("Reject Application", 
            $"Reject {sitterName}'s application?", "Reject", "Cancel");

        if (!confirm) return;

        var reason = await Shell.Current.DisplayPromptAsync("Rejection Reason", 
            "Please provide a reason for rejection (optional):", "Reject", "Cancel", 
            placeholder: "e.g., Found a better match");

        await ExecuteAsync(async () =>
        {
            var success = await _bookingService.RejectBookingAsync(application.Id, 
                reason ?? "Application was not selected");

            if (success)
            {
                await Shell.Current.DisplayAlert("Success", "Application rejected", "OK");
                await LoadApplicationsAsync();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to reject application", "OK");
            }
        });
    }

    [RelayCommand]
    private async Task ViewApplicationDetailsAsync(Booking application)
    {
        if (application == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "BookingId", application.Id }
        };

        await Shell.Current.GoToAsync("bookingdetails", navigationParameter);
    }

    [RelayCommand]
    private async Task MessageSitterAsync(Booking application)
    {
        if (application == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "BookingId", application.Id }
        };

        await Shell.Current.GoToAsync("chat", navigationParameter);
    }

    [RelayCommand]
    private async Task ViewSitterReviewsAsync(Booking application)
    {
        if (application?.Sitter == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "UserId", application.SitterId },
            { "ReviewType", "Received" }
        };

        await Shell.Current.GoToAsync("userreviews", navigationParameter);
    }

    [RelayCommand]
    private async Task RefreshApplicationsAsync()
    {
        await LoadApplicationsAsync();
    }

    partial void OnRequestIdChanged(int value)
    {
        if (value > 0)
        {
            Task.Run(async () => await LoadApplicationsAsync());
        }
    }
}
