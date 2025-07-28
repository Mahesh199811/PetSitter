using Microsoft.AspNetCore.Identity;
using PetSitterConnect.Models;
using PetSitterConnect.Interfaces;

namespace PetSitterConnect.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private User? _currentUser;

    public AuthService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<AuthResult> RegisterAsync(RegisterModel model)
    {
        try
        {
            if (model.Password != model.ConfirmPassword)
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "Passwords do not match",
                    Errors = new List<string> { "Passwords do not match" }
                };
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "User with this email already exists",
                    Errors = new List<string> { "User with this email already exists" }
                };
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserType = model.UserType,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                City = model.City,
                PostalCode = model.PostalCode,
                Country = model.Country,
                DateJoined = DateTime.UtcNow,
                LastActive = DateTime.UtcNow,
                IsActive = true,
                IsAvailable = model.UserType == UserType.PetSitter || model.UserType == UserType.Both
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _currentUser = user;
                return new AuthResult
                {
                    Success = true,
                    Message = "Registration successful",
                    User = user
                };
            }

            return new AuthResult
            {
                Success = false,
                Message = "Registration failed",
                Errors = result.Errors.Select(e => e.Description).ToList()
            };
        }
        catch (Exception ex)
        {
            return new AuthResult
            {
                Success = false,
                Message = "An error occurred during registration",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<AuthResult> LoginAsync(LoginModel model)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "Invalid email or password",
                    Errors = new List<string> { "Invalid email or password" }
                };
            }

            if (!user.IsActive)
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "Account is deactivated",
                    Errors = new List<string> { "Account is deactivated" }
                };
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);

            if (passwordValid)
            {
                user.LastActive = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                _currentUser = user;

                // Note: In API, session management is handled via JWT tokens
                // No need to store in preferences like in MAUI app

                return new AuthResult
                {
                    Success = true,
                    Message = "Login successful",
                    User = user
                };
            }

            return new AuthResult
            {
                Success = false,
                Message = "Invalid email or password",
                Errors = new List<string> { "Invalid email or password" }
            };
        }
        catch (Exception ex)
        {
            return new AuthResult
            {
                Success = false,
                Message = "An error occurred during login",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public Task<bool> LogoutAsync()
    {
        try
        {
            _currentUser = null;
            // Note: In API, logout is handled by client discarding JWT token
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        if (_currentUser != null)
            return _currentUser;

        // Note: In API, current user should be obtained from JWT token claims
        // This method should be updated to work with HttpContext.User
        return null; // Placeholder - implement JWT-based user retrieval
    }

    public async Task<bool> IsLoggedInAsync()
    {
        var user = await GetCurrentUserAsync();
        return user != null;
    }

    public async Task<AuthResult> ChangePasswordAsync(ChangePasswordModel model)
    {
        try
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "User not found",
                    Errors = new List<string> { "User not found" }
                };
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "Passwords do not match",
                    Errors = new List<string> { "Passwords do not match" }
                };
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                return new AuthResult
                {
                    Success = true,
                    Message = "Password changed successfully"
                };
            }

            return new AuthResult
            {
                Success = false,
                Message = "Failed to change password",
                Errors = result.Errors.Select(e => e.Description).ToList()
            };
        }
        catch (Exception ex)
        {
            return new AuthResult
            {
                Success = false,
                Message = "An error occurred while changing password",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return false; // Don't reveal that user doesn't exist

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            // TODO: Send email with reset token
            // For now, just return true
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AuthResult> ResetPasswordAsync(ResetPasswordModel model)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "User not found",
                    Errors = new List<string> { "User not found" }
                };
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "Passwords do not match",
                    Errors = new List<string> { "Passwords do not match" }
                };
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (result.Succeeded)
            {
                return new AuthResult
                {
                    Success = true,
                    Message = "Password reset successful"
                };
            }

            return new AuthResult
            {
                Success = false,
                Message = "Failed to reset password",
                Errors = result.Errors.Select(e => e.Description).ToList()
            };
        }
        catch (Exception ex)
        {
            return new AuthResult
            {
                Success = false,
                Message = "An error occurred while resetting password",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<bool> UpdateProfileAsync(User user)
    {
        try
        {
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _currentUser = user;
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}
