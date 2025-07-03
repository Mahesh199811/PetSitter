using PetSitterConnect.ViewModels;

namespace PetSitterConnect.Views;

public partial class ChatPage : ContentPage
{
    public ChatPage(ChatViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is ChatViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }

    private void OnMessageAdded()
    {
        // Scroll to bottom when new message is added
        if (MessagesCollectionView.ItemsSource is System.Collections.IList items && items.Count > 0)
        {
            MessagesCollectionView.ScrollTo(items.Count - 1, position: ScrollToPosition.End, animate: true);
        }
    }
}
