using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetSitterConnect.Models;
using PetSitterConnect.Services;
using PetSitterConnect.Interfaces;
using System.Collections.ObjectModel;

namespace PetSitterConnect.ViewModels;

[QueryProperty(nameof(StartDateParam), "StartDate")]
[QueryProperty(nameof(EndDateParam), "EndDate")]
public partial class CreatePetCareRequestViewModel : BaseViewModel
{
    private readonly IPetCareRequestService _petCareRequestService;
    private readonly IPetService _petService;
    private readonly IAuthService _authService;

    public CreatePetCareRequestViewModel(
        IPetCareRequestService petCareRequestService,
        IPetService petService,
        IAuthService authService)
    {
        _petCareRequestService = petCareRequestService;
        _petService = petService;
        _authService = authService;
        Title = "Create Pet Care Request";
        
        UserPets = new ObservableCollection<Pet>();
        StartDate = DateTime.Today.AddDays(1);
        EndDate = DateTime.Today.AddDays(2);
        CareType = CareType.PetSitting;
    }

    [ObservableProperty]
    private ObservableCollection<Pet> userPets;

    [ObservableProperty]
    private Pet? selectedPet;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private DateTime startDate;

    [ObservableProperty]
    private DateTime endDate;

    [ObservableProperty]
    private CareType careType;

    [ObservableProperty]
    private decimal budget;

    [ObservableProperty]
    private string location = string.Empty;

    [ObservableProperty]
    private string specialInstructions = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    // Navigation parameters
    public DateTime StartDateParam
    {
        set => StartDate = value;
    }

    public DateTime EndDateParam
    {
        set => EndDate = value;
    }

    public List<CareType> CareTypes { get; } = new()
    {
        CareType.PetSitting,
        CareType.PetBoarding,
        CareType.DogWalking,
        CareType.Daycare,
        CareType.Overnight
    };

    public override async Task InitializeAsync()
    {
        await LoadUserPetsAsync();
    }

    [RelayCommand]
    private async Task LoadUserPetsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                System.Diagnostics.Debug.WriteLine("No current user found");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Loading pets for user: {currentUser.Id}");
            var pets = await _petService.GetPetsByOwnerAsync(currentUser.Id);

            System.Diagnostics.Debug.WriteLine($"Found {pets.Count()} pets");

            UserPets.Clear();
            foreach (var pet in pets)
            {
                UserPets.Add(pet);
                System.Diagnostics.Debug.WriteLine($"Added pet: {pet.Name}");
            }

            if (UserPets.Any() && SelectedPet == null)
            {
                SelectedPet = UserPets.First();
            }
            else if (!UserPets.Any())
            {
                System.Diagnostics.Debug.WriteLine("No pets found for user");
            }
        });
    }

    [RelayCommand]
    private async Task CreateRequestAsync()
    {
        if (!ValidateInput())
            return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = string.Empty;

            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                ErrorMessage = "User not authenticated";
                return;
            }

            if (SelectedPet == null)
            {
                ErrorMessage = "Please select a pet";
                return;
            }

            var request = new PetCareRequest
            {
                Title = Title,
                Description = Description,
                StartDate = StartDate,
                EndDate = EndDate,
                CareType = CareType,
                Budget = Budget,
                Location = Location,
                SpecialInstructions = SpecialInstructions,
                OwnerId = currentUser.Id,
                PetId = SelectedPet.Id,
                Latitude = currentUser.Latitude,
                Longitude = currentUser.Longitude
            };

            var createdRequest = await _petCareRequestService.CreateRequestAsync(request);

            await Shell.Current.DisplayAlert("Success", 
                "Pet care request created successfully!", "OK");

            await Shell.Current.GoToAsync("..");
        });
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task AddPetAsync()
    {
        await Shell.Current.GoToAsync("addpet");
    }

    private bool ValidateInput()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Title))
        {
            ErrorMessage = "Title is required";
            return false;
        }

        if (SelectedPet == null)
        {
            ErrorMessage = "Please select a pet";
            return false;
        }

        if (StartDate < DateTime.Today)
        {
            ErrorMessage = "Start date cannot be in the past";
            return false;
        }

        if (EndDate <= StartDate)
        {
            ErrorMessage = "End date must be after start date";
            return false;
        }

        if (Budget <= 0)
        {
            ErrorMessage = "Budget must be greater than 0";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Location))
        {
            ErrorMessage = "Location is required";
            return false;
        }

        return true;
    }

    partial void OnStartDateChanged(DateTime value)
    {
        if (EndDate <= value)
        {
            EndDate = value.AddDays(1);
        }
    }
}
