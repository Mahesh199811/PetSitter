using PetSitterConnect.ViewModels;

namespace PetSitterConnect.Views;

public partial class PetCareRequestDetailPage : ContentPage
{
    public PetCareRequestDetailPage(PetCareRequestDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is PetCareRequestDetailViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
