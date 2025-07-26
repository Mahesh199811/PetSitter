using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetSitterConnect.Models;
using PetSitterConnect.Services;
using PetSitterConnect.Interfaces;

namespace PetSitterConnect.ViewModels;

[QueryProperty(nameof(RequestId), "RequestId")]
public partial class PetCareRequestDetailViewModel : BaseViewModel
{
    private readonly IPetCareRequestService _petCareRequestService;
    private readonly IBookingService _bookingService;
    private readonly IAuthService _authService;

    public PetCareRequestDetailViewModel(
        IPetCareRequestService petCareRequestService,
        IBookingService bookingService,
        IAuthService authService)
    {
        _petCareRequestService = petCareRequestService;
        _bookingService = bookingService;
        _authService = authService;
        Title = "Request Details";
    }

    [ObservableProperty]
    private int requestId;

    [ObservableProperty]
    private PetCareRequest? petCareRequest;

    [ObservableProperty]
    private User? currentUser;

    [ObservableProperty]
    private bool canApply = true;

    [ObservableProperty]
    private bool isOwner;

    [ObservableProperty]
    private bool hasApplied;

    [ObservableProperty]
    private Booking? existingBooking;

    [ObservableProperty]
    private string applicationStatus = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string applicationNotes = string.Empty;

    [ObservableProperty]
    private string userRoleIcon = "👤";

    [ObservableProperty]
    private string userRoleLabel = "USER";

    [ObservableProperty]
    private Color userRoleColor = Colors.Gray;

    [ObservableProperty]
    private bool showSitterActions;

    [ObservableProperty]
    private string actionButtonText = "Apply";

    [ObservableProperty]
    private Color actionButtonColor = Colors.Blue;

    public override async Task InitializeAsync()
    {
        await LoadRequestDetailsAsync();
    }

    [RelayCommand]
    private async Task LoadRequestDetailsAsync()
    {
        await ExecuteAsync(async () =>
        {
            if (RequestId <= 0) return;

            CurrentUser = await _authService.GetCurrentUserAsync();
            PetCareRequest = await _petCareRequestService.GetRequestByIdAsync(RequestId);

            if (PetCareRequest != null && CurrentUser != null)
            {
                UpdateUserRoleDisplay();

                IsOwner = PetCareRequest.OwnerId == CurrentUser.Id;
                ShowSitterActions = !IsOwner && (CurrentUser.UserType == UserType.PetSitter || CurrentUser.UserType == UserType.Both);

                // Check if current user has already applied
                ExistingBooking = await _bookingService.GetBookingByRequestAndSitterAsync(RequestId, CurrentUser.Id);
                HasApplied = ExistingBooking != null;

                CanApply = !IsOwner &&
                          PetCareRequest.Status == RequestStatus.Open &&
                          PetCareRequest.EndDate > DateTime.UtcNow &&
                          ShowSitterActions;

                UpdateApplicationStatus();
            }
        });
    }

    [RelayCommand]
    private async Task ApplyForRequestAsync()
    {
        if (PetCareRequest == null || CurrentUser == null)
            return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = string.Empty;

            if (ExistingBooking?.Status == BookingStatus.Pending)
            {
                // Withdraw application
                var confirm = await Shell.Current.DisplayAlert("Withdraw Application",
                    "Are you sure you want to withdraw your application?", "Yes", "No");

                if (confirm)
                {
                    await _bookingService.CancelBookingAsync(ExistingBooking.Id, "Application withdrawn by sitter");
                    await LoadRequestDetailsAsync(); // Refresh
                }
            }
            else if (ExistingBooking?.Status == BookingStatus.Confirmed)
            {
                // Open chat
                await OpenChatAsync();
            }
            else
            {
                // Apply for request
                var booking = new Booking
                {
                    PetCareRequestId = PetCareRequest.Id,
                    SitterId = CurrentUser.Id,
                    OwnerId = PetCareRequest.OwnerId,
                    TotalAmount = PetCareRequest.Budget,
                    Notes = ApplicationNotes,
                    Status = BookingStatus.Pending
                };

                var createdBooking = await _bookingService.CreateBookingAsync(booking);

                await Shell.Current.DisplayAlert("Success",
                    "Your application has been submitted! The pet owner will review it soon.", "OK");

                await LoadRequestDetailsAsync(); // Refresh
            }
        });
    }

    [RelayCommand]
    private async Task EditRequestAsync()
    {
        if (PetCareRequest == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "RequestId", PetCareRequest.Id }
        };

        await Shell.Current.GoToAsync("editrequest", navigationParameter);
    }

    [RelayCommand]
    private async Task DeleteRequestAsync()
    {
        if (PetCareRequest == null) return;

        var confirm = await Shell.Current.DisplayAlert("Confirm Delete", 
            "Are you sure you want to delete this request? This action cannot be undone.", 
            "Delete", "Cancel");

        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            var success = await _petCareRequestService.DeleteRequestAsync(PetCareRequest.Id);
            
            if (success)
            {
                await Shell.Current.DisplayAlert("Success", 
                    "Request deleted successfully", "OK");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                ErrorMessage = "Failed to delete request";
            }
        });
    }

    [RelayCommand]
    private async Task ViewApplicationsAsync()
    {
        if (PetCareRequest == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "RequestId", PetCareRequest.Id }
        };

        await Shell.Current.GoToAsync("requestapplications", navigationParameter);
    }

    [RelayCommand]
    private async Task ContactOwnerAsync()
    {
        if (PetCareRequest?.Owner == null) return;

        // TODO: Implement messaging functionality
        await Shell.Current.DisplayAlert("Contact Owner", 
            $"Contact {PetCareRequest.Owner.FullName} at {PetCareRequest.Owner.Email}", "OK");
    }

    [RelayCommand]
    private async Task ShareRequestAsync()
    {
        if (PetCareRequest == null) return;

        var shareText = $"Check out this pet care request: {PetCareRequest.Title}\n" +
                       $"Pet: {PetCareRequest.Pet.Name} ({PetCareRequest.Pet.Type})\n" +
                       $"Dates: {PetCareRequest.StartDate:MMM dd} - {PetCareRequest.EndDate:MMM dd}\n" +
                       $"Budget: ${PetCareRequest.Budget:F2}";

        await Share.RequestAsync(new ShareTextRequest
        {
            Text = shareText,
            Title = "Pet Care Request"
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

    private void UpdateApplicationStatus()
    {
        if (ExistingBooking == null)
        {
            ApplicationStatus = "Not Applied";
            ActionButtonText = "Apply for this Request";
            ActionButtonColor = Color.FromArgb("#4CAF50"); // Green
        }
        else
        {
            ApplicationStatus = ExistingBooking.Status switch
            {
                BookingStatus.Pending => "Application Pending",
                BookingStatus.Confirmed => "Application Accepted",
                BookingStatus.Rejected => "Application Declined",
                BookingStatus.InProgress => "Booking In Progress",
                BookingStatus.Completed => "Booking Completed",
                BookingStatus.Cancelled => "Booking Cancelled",
                _ => "Unknown Status"
            };

            ActionButtonText = ExistingBooking.Status switch
            {
                BookingStatus.Pending => "Withdraw Application",
                BookingStatus.Confirmed => "Start Chat",
                BookingStatus.Rejected => "Apply Again",
                _ => "View Booking"
            };

            ActionButtonColor = ExistingBooking.Status switch
            {
                BookingStatus.Pending => Color.FromArgb("#FF9800"), // Orange
                BookingStatus.Confirmed => Color.FromArgb("#2196F3"), // Blue
                BookingStatus.Rejected => Color.FromArgb("#4CAF50"), // Green
                _ => Colors.Gray
            };

            CanApply = ExistingBooking.Status == BookingStatus.Rejected;
        }
    }

    [RelayCommand]
    private async Task OpenChatAsync()
    {
        if (ExistingBooking == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "BookingId", ExistingBooking.Id }
        };

        await Shell.Current.GoToAsync("chat", navigationParameter);
    }

    partial void OnRequestIdChanged(int value)
    {
        if (value > 0)
        {
            Task.Run(async () => await LoadRequestDetailsAsync());
        }
    }
}
