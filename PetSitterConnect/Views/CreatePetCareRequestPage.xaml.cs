using PetSitterConnect.ViewModels;

namespace PetSitterConnect.Views;

public partial class CreatePetCareRequestPage : ContentPage
{
    public CreatePetCareRequestPage(CreatePetCareRequestViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is CreatePetCareRequestViewModel viewModel)
        {
            await viewModel.InitializeAsync();
            // Refresh pets list in case user added a new pet
            await viewModel.LoadUserPetsCommand.ExecuteAsync(null);
        }
    }
}
