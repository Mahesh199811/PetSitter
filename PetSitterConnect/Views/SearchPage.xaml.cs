using PetSitterConnect.ViewModels;

namespace PetSitterConnect.Views;

public partial class SearchPage : ContentPage
{
    public SearchPage(SearchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}