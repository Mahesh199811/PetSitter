using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetSitterConnect.Models;
using PetSitterConnect.Services;
using PetSitterConnect.Interfaces;

namespace PetSitterConnect.ViewModels;

public partial class AddPetViewModel : BaseViewModel
{
    private readonly IPetService _petService;
    private readonly IAuthService _authService;

    public AddPetViewModel(IPetService petService, IAuthService authService)
    {
        _petService = petService;
        _authService = authService;
        Title = "Add Pet";
        
        // Set default values
        Type = PetType.Dog;
        Gender = "Male";
        Size = "Medium";
        IsVaccinated = true;
        IsFriendlyWithChildren = true;
        IsFriendlyWithOtherPets = true;
    }

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private PetType type;

    [ObservableProperty]
    private string breed = string.Empty;

    [ObservableProperty]
    private int age;

    [ObservableProperty]
    private string gender;

    [ObservableProperty]
    private double weight;

    [ObservableProperty]
    private string size;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string specialNeeds = string.Empty;

    [ObservableProperty]
    private string medicalConditions = string.Empty;

    [ObservableProperty]
    private string medications = string.Empty;

    [ObservableProperty]
    private string feedingInstructions = string.Empty;

    [ObservableProperty]
    private bool isVaccinated;

    [ObservableProperty]
    private bool isNeutered;

    [ObservableProperty]
    private bool isFriendlyWithOtherPets;

    [ObservableProperty]
    private bool isFriendlyWithChildren;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public List<PetType> PetTypes { get; } = new()
    {
        PetType.Dog,
        PetType.Cat,
        PetType.Bird,
        PetType.Fish,
        PetType.Rabbit,
        PetType.Hamster,
        PetType.GuineaPig,
        PetType.Reptile,
        PetType.Other
    };

    public List<string> Genders { get; } = new() { "Male", "Female" };
    public List<string> Sizes { get; } = new() { "Small", "Medium", "Large" };

    [RelayCommand]
    private async Task SavePetAsync()
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

            var pet = new Pet
            {
                Name = Name,
                Type = Type,
                Breed = string.IsNullOrWhiteSpace(Breed) ? null : Breed,
                Age = Age > 0 ? Age : null,
                Gender = Gender,
                Weight = Weight > 0 ? Weight : null,
                Size = Size,
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description,
                SpecialNeeds = string.IsNullOrWhiteSpace(SpecialNeeds) ? null : SpecialNeeds,
                MedicalConditions = string.IsNullOrWhiteSpace(MedicalConditions) ? null : MedicalConditions,
                Medications = string.IsNullOrWhiteSpace(Medications) ? null : Medications,
                FeedingInstructions = string.IsNullOrWhiteSpace(FeedingInstructions) ? null : FeedingInstructions,
                IsVaccinated = IsVaccinated,
                IsNeutered = IsNeutered,
                IsFriendlyWithOtherPets = IsFriendlyWithOtherPets,
                IsFriendlyWithChildren = IsFriendlyWithChildren,
                OwnerId = currentUser.Id
            };

            var createdPet = await _petService.CreatePetAsync(pet);

            await Shell.Current.DisplayAlert("Success", 
                $"{pet.Name} has been added successfully!", "OK");

            await Shell.Current.GoToAsync("..");
        });
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    private bool ValidateInput()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Pet name is required";
            return false;
        }

        if (Name.Length > 100)
        {
            ErrorMessage = "Pet name must be less than 100 characters";
            return false;
        }

        if (Age < 0 || Age > 30)
        {
            ErrorMessage = "Please enter a valid age (0-30 years)";
            return false;
        }

        if (Weight < 0 || Weight > 200)
        {
            ErrorMessage = "Please enter a valid weight (0-200 kg)";
            return false;
        }

        return true;
    }
}
