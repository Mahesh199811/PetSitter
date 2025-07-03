using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetSitterConnect.Models;
using PetSitterConnect.Services;
using System.Collections.ObjectModel;

namespace PetSitterConnect.ViewModels;

public partial class PetCareRequestListViewModel : BaseViewModel
{
    private readonly IPetCareRequestService _petCareRequestService;
    private readonly IAuthService _authService;

    public PetCareRequestListViewModel(
        IPetCareRequestService petCareRequestService,
        IAuthService authService)
    {
        _petCareRequestService = petCareRequestService;
        _authService = authService;
        Title = "Pet Care Requests";
        
        PetCareRequests = new ObservableCollection<PetCareRequest>();
        FilteredRequests = new ObservableCollection<PetCareRequest>();
    }

    [ObservableProperty]
    private ObservableCollection<PetCareRequest> petCareRequests;

    [ObservableProperty]
    private ObservableCollection<PetCareRequest> filteredRequests;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private CareType? selectedCareType;

    [ObservableProperty]
    private bool showMyRequestsOnly;

    [ObservableProperty]
    private PetCareRequest? selectedRequest;

    [ObservableProperty]
    private User? currentUser;

    [ObservableProperty]
    private string userRoleIcon = "👤";

    [ObservableProperty]
    private string userRoleLabel = "USER";

    [ObservableProperty]
    private Color userRoleColor = Colors.Gray;

    [ObservableProperty]
    private bool canCreateRequests = true;

    [ObservableProperty]
    private string contextDescription = string.Empty;

    public List<CareType?> CareTypes { get; } = new()
    {
        null, // All types
        CareType.PetSitting,
        CareType.PetBoarding,
        CareType.DogWalking,
        CareType.Daycare,
        CareType.Overnight
    };

    public override async Task InitializeAsync()
    {
        await LoadUserInfoAsync();
        await LoadRequestsAsync();
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

        // Update title and permissions based on role
        Title = CurrentUser.UserType switch
        {
            UserType.PetOwner => "Find Sitters",
            UserType.PetSitter => "Available Requests",
            UserType.Both => "Pet Care Requests",
            _ => "Pet Care Requests"
        };

        CanCreateRequests = CurrentUser.UserType == UserType.PetOwner || CurrentUser.UserType == UserType.Both;

        ContextDescription = CurrentUser.UserType switch
        {
            UserType.PetOwner => "Create requests to find pet sitters",
            UserType.PetSitter => "Browse and apply for pet care opportunities",
            UserType.Both => "Manage your requests and find opportunities",
            _ => "Browse pet care requests"
        };
    }

    [RelayCommand]
    private async Task LoadRequestsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null) return;

            IEnumerable<PetCareRequest> requests;

            if (ShowMyRequestsOnly)
            {
                requests = await _petCareRequestService.GetRequestsByOwnerAsync(currentUser.Id);
            }
            else
            {
                // Get available requests for sitters
                requests = await _petCareRequestService.GetAvailableRequestsAsync(
                    currentUser.Latitude, 
                    currentUser.Longitude);
            }

            PetCareRequests.Clear();
            foreach (var request in requests)
            {
                PetCareRequests.Add(request);
            }

            ApplyFilters();
        });
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await ExecuteAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                ApplyFilters();
                return;
            }

            var searchResults = await _petCareRequestService.SearchRequestsAsync(
                SearchText, 
                SelectedCareType);

            PetCareRequests.Clear();
            foreach (var request in searchResults)
            {
                PetCareRequests.Add(request);
            }

            ApplyFilters();
        });
    }

    [RelayCommand]
    private void ApplyFilters()
    {
        var filtered = PetCareRequests.AsEnumerable();

        if (SelectedCareType.HasValue)
        {
            filtered = filtered.Where(r => r.CareType == SelectedCareType.Value);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLower();
            filtered = filtered.Where(r => 
                r.Title.ToLower().Contains(searchLower) ||
                r.Description?.ToLower().Contains(searchLower) == true ||
                r.Location?.ToLower().Contains(searchLower) == true ||
                r.Pet.Name.ToLower().Contains(searchLower));
        }

        FilteredRequests.Clear();
        foreach (var request in filtered.OrderByDescending(r => r.CreatedAt))
        {
            FilteredRequests.Add(request);
        }
    }

    [RelayCommand]
    private async Task CreateRequestAsync()
    {
        await Shell.Current.GoToAsync("createrequest");
    }

    [RelayCommand]
    private async Task OpenCalendarAsync()
    {
        await Shell.Current.GoToAsync("calendar");
    }

    [RelayCommand]
    private async Task ViewRequestDetailsAsync(PetCareRequest request)
    {
        if (request == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "RequestId", request.Id }
        };

        await Shell.Current.GoToAsync("requestdetails", navigationParameter);
    }

    [RelayCommand]
    private async Task ToggleMyRequestsAsync()
    {
        ShowMyRequestsOnly = !ShowMyRequestsOnly;
        await LoadRequestsAsync();
    }

    partial void OnSelectedCareTypeChanged(CareType? value)
    {
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ApplyFilters();
        }
    }
}
