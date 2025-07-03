using PetSitterConnect.ViewModels;

namespace PetSitterConnect.Views;

public partial class AddPetPage : ContentPage
{
    public AddPetPage(AddPetViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
