using PetSitterConnect.ViewModels;

namespace PetSitterConnect.Views;

public partial class ReviewListPage : ContentPage
{
    public ReviewListPage(ReviewListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}