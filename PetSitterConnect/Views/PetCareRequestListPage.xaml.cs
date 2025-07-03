using PetSitterConnect.ViewModels;

namespace PetSitterConnect.Views;

public partial class PetCareRequestListPage : ContentPage
{
    public PetCareRequestListPage(PetCareRequestListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is PetCareRequestListViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
