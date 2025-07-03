using PetSitterConnect.Views;
using PetSitterConnect.Models;
using PetSitterConnect.Services;
using PetSitterConnect.Helpers;

namespace PetSitterConnect;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register routes for navigation
		Routing.RegisterRoute("login", typeof(LoginPage));
		Routing.RegisterRoute("register", typeof(RegisterPage));
		Routing.RegisterRoute("createrequest", typeof(CreatePetCareRequestPage));
		Routing.RegisterRoute("bookingdetails", typeof(BookingDetailPage));
		Routing.RegisterRoute("addpet", typeof(AddPetPage));
		Routing.RegisterRoute("chat", typeof(ChatPage));
		Routing.RegisterRoute("calendar", typeof(CalendarBookingPage));
		Routing.RegisterRoute("requestdetails", typeof(PetCareRequestDetailPage));
	}

	public void UpdateNavigationForUserType(UserType userType)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			switch (userType)
			{
				case UserType.PetOwner:
					// Show Find Sitters, hide Available Requests
					FindSittersTab.IsVisible = true;
					AvailableRequestsTab.IsVisible = false;
					break;

				case UserType.PetSitter:
					// Show Available Requests, hide Find Sitters
					FindSittersTab.IsVisible = false;
					AvailableRequestsTab.IsVisible = true;
					break;

				case UserType.Both:
					// Show both tabs
					FindSittersTab.IsVisible = true;
					AvailableRequestsTab.IsVisible = true;
					break;

				default:
					// Default to pet owner view
					FindSittersTab.IsVisible = true;
					AvailableRequestsTab.IsVisible = false;
					break;
			}
		});
	}
}
