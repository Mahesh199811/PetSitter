using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetSitterConnect.Models;
using PetSitterConnect.Services;
using PetSitterConnect.Interfaces;

namespace PetSitterConnect.ViewModels;

public partial class RegisterViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    public RegisterViewModel(IAuthService authService)
    {
        _authService = authService;
        Title = "Register";
        UserType = UserType.PetOwner;
    }

    [ObservableProperty]
    private string firstName = string.Empty;

    [ObservableProperty]
    private string lastName = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string confirmPassword = string.Empty;

    [ObservableProperty]
    private UserType userType;

    [ObservableProperty]
    private string phoneNumber = string.Empty;

    [ObservableProperty]
    private string address = string.Empty;

    [ObservableProperty]
    private string city = string.Empty;

    [ObservableProperty]
    private string postalCode = string.Empty;

    [ObservableProperty]
    private string country = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool acceptTerms;

    public List<UserType> UserTypes { get; } = new()
    {
        UserType.PetOwner,
        UserType.PetSitter,
        UserType.Both
    };

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (!ValidateInput())
            return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = string.Empty;

            var registerModel = new RegisterModel
            {
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                Password = Password,
                ConfirmPassword = ConfirmPassword,
                UserType = UserType,
                PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber,
                Address = string.IsNullOrWhiteSpace(Address) ? null : Address,
                City = string.IsNullOrWhiteSpace(City) ? null : City,
                PostalCode = string.IsNullOrWhiteSpace(PostalCode) ? null : PostalCode,
                Country = string.IsNullOrWhiteSpace(Country) ? null : Country
            };

            var result = await _authService.RegisterAsync(registerModel);

            if (result.Success)
            {
                // Update navigation based on user role
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser != null && Shell.Current is AppShell appShell)
                {
                    appShell.UpdateNavigationForUserType(currentUser.UserType);
                }

                await Shell.Current.DisplayAlert("Success",
                    "Registration successful! Welcome to PetSitter Connect!", "OK");
                await Shell.Current.GoToAsync("//main");
            }
            else
            {
                ErrorMessage = result.Message;
                if (result.Errors.Any())
                {
                    ErrorMessage = string.Join("\n", result.Errors);
                }
            }
        });
    }

    [RelayCommand]
    private async Task NavigateToLoginAsync()
    {
        await Shell.Current.GoToAsync("//login");
    }

    private bool ValidateInput()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(FirstName))
        {
            ErrorMessage = "First name is required";
            return false;
        }

        if (string.IsNullOrWhiteSpace(LastName))
        {
            ErrorMessage = "Last name is required";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Email is required";
            return false;
        }

        if (!IsValidEmail(Email))
        {
            ErrorMessage = "Please enter a valid email address";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Password is required";
            return false;
        }

        if (Password.Length < 6)
        {
            ErrorMessage = "Password must be at least 6 characters";
            return false;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match";
            return false;
        }

        if (!AcceptTerms)
        {
            ErrorMessage = "Please accept the terms and conditions";
            return false;
        }

        return true;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
