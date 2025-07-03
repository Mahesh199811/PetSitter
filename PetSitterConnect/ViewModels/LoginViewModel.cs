using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetSitterConnect.Services;

namespace PetSitterConnect.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
        Title = "Login";
    }

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool rememberMe;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (!ValidateInput())
            return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = string.Empty;

            var loginModel = new LoginModel
            {
                Email = Email,
                Password = Password,
                RememberMe = RememberMe
            };

            var result = await _authService.LoginAsync(loginModel);

            if (result.Success)
            {
                // Update navigation based on user role
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser != null && Shell.Current is AppShell appShell)
                {
                    appShell.UpdateNavigationForUserType(currentUser.UserType);
                }

                // Navigate to main page
                await Shell.Current.GoToAsync("//main");
            }
            else
            {
                ErrorMessage = result.Message;
            }
        });
    }

    [RelayCommand]
    private async Task NavigateToRegisterAsync()
    {
        await Shell.Current.GoToAsync("//register");
    }

    [RelayCommand]
    private async Task ForgotPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Please enter your email address first";
            return;
        }

        await ExecuteAsync(async () =>
        {
            var success = await _authService.ForgotPasswordAsync(Email);
            if (success)
            {
                await Shell.Current.DisplayAlert("Success", 
                    "Password reset instructions have been sent to your email", "OK");
            }
            else
            {
                ErrorMessage = "Failed to send password reset email";
            }
        });
    }

    private bool ValidateInput()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Email is required";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Password is required";
            return false;
        }

        if (!IsValidEmail(Email))
        {
            ErrorMessage = "Please enter a valid email address";
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
