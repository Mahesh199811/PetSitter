using PetSitterConnect.ViewModels;

namespace PetSitterConnect.Views;

public partial class CalendarBookingPage : ContentPage
{
    public CalendarBookingPage(CalendarBookingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is CalendarBookingViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
