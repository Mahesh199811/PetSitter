using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PetSitterConnect.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private bool isRefreshing;

    public virtual async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    protected async Task ExecuteAsync(Func<Task> operation, string? loadingMessage = null)
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            await operation();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, T defaultValue = default!)
    {
        if (IsBusy)
            return defaultValue;

        try
        {
            IsBusy = true;
            return await operation();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
            return defaultValue;
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected virtual async Task HandleErrorAsync(Exception ex)
    {
        // Log error
        System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
        
        // Show error to user
        await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
    }

    [RelayCommand]
    protected virtual async Task RefreshAsync()
    {
        IsRefreshing = true;
        try
        {
            await InitializeAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }
}
