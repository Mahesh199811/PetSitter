using PetSitterConnect.ViewModels;

namespace PetSitterConnect.Views;

public partial class BookingListPage : ContentPage
{
    public BookingListPage(BookingListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is BookingListViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
