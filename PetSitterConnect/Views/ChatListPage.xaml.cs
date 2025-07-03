using PetSitterConnect.ViewModels;

namespace PetSitterConnect.Views;

public partial class ChatListPage : ContentPage
{
    public ChatListPage(ChatListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is ChatListViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
