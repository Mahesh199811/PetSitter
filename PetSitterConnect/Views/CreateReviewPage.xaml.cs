using PetSitterConnect.ViewModels;

namespace PetSitterConnect.Views;

public partial class CreateReviewPage : ContentPage
{
    public CreateReviewPage(CreateReviewViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}