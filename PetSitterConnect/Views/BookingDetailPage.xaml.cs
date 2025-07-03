using PetSitterConnect.ViewModels;

namespace PetSitterConnect.Views;

public partial class BookingDetailPage : ContentPage
{
    public BookingDetailPage(BookingDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is BookingDetailViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
