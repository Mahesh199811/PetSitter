using PetSitterConnect.Services;

namespace PetSitterConnect;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new AppShell());

		// Initialize database when app starts
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			try
			{
				// Wait a bit for the app to fully load
				await Task.Delay(1000);

				var serviceProvider = Handler?.MauiContext?.Services;
				if (serviceProvider != null)
				{
					var databaseService = serviceProvider.GetService<IDatabaseService>();
					if (databaseService != null)
					{
						await databaseService.InitializeAsync();
						System.Diagnostics.Debug.WriteLine("Database initialized successfully");
					}
				}

				// Navigate to login page
				await Shell.Current.GoToAsync("//login");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Database initialization failed: {ex.Message}");
				// Still navigate to login even if database init fails
				await Shell.Current.GoToAsync("//login");
			}
		});

		return window;
	}
}