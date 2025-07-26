using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using PetSitterConnect.Data;
using PetSitterConnect.Models;
using PetSitterConnect.Services;
using PetSitterConnect.Interfaces;
using PetSitterConnect.ViewModels;
using PetSitterConnect.Views;
using CommunityToolkit.Maui;

namespace PetSitterConnect;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Configure Entity Framework
		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "petsitter.db");
		builder.Services.AddDbContext<PetSitterDbContext>(options =>
			options.UseSqlite($"Data Source={dbPath}"));

		// Configure Identity Core (simplified for MAUI)
		builder.Services.AddIdentityCore<User>(options =>
		{
			// Password settings
			options.Password.RequireDigit = true;
			options.Password.RequireLowercase = true;
			options.Password.RequireNonAlphanumeric = false;
			options.Password.RequireUppercase = true;
			options.Password.RequiredLength = 6;
			options.Password.RequiredUniqueChars = 1;

			// User settings
			options.User.AllowedUserNameCharacters =
				"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
			options.User.RequireUniqueEmail = true;
		})
		.AddEntityFrameworkStores<PetSitterDbContext>();

		// Register services
		builder.Services.AddScoped<IDatabaseService, DatabaseService>();
		builder.Services.AddScoped<IAuthService, AuthService>();
		builder.Services.AddScoped<IUserService, UserService>();
		builder.Services.AddScoped<IPetService, PetService>();
		builder.Services.AddScoped<IPetCareRequestService, PetCareRequestService>();
		builder.Services.AddScoped<ISchedulingService, SchedulingService>();
		builder.Services.AddScoped<INotificationService, NotificationService>();
		builder.Services.AddScoped<IChatService, ChatService>();
		builder.Services.AddScoped<IBookingService, BookingService>();

		// Register ViewModels
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<RegisterViewModel>();
		builder.Services.AddTransient<PetCareRequestListViewModel>();
		builder.Services.AddTransient<CreatePetCareRequestViewModel>();
		builder.Services.AddTransient<PetCareRequestDetailViewModel>();
		builder.Services.AddTransient<BookingListViewModel>();
		builder.Services.AddTransient<BookingDetailViewModel>();
		builder.Services.AddTransient<BookingApplicationsViewModel>();
		builder.Services.AddTransient<AddPetViewModel>();
		builder.Services.AddTransient<ChatListViewModel>();
		builder.Services.AddTransient<ChatViewModel>();
		builder.Services.AddTransient<CalendarBookingViewModel>();
		builder.Services.AddTransient<PetCareRequestDetailViewModel>();

		// Register Views
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<RegisterPage>();
		builder.Services.AddTransient<PetCareRequestListPage>();
		builder.Services.AddTransient<CreatePetCareRequestPage>();
		builder.Services.AddTransient<BookingListPage>();
		builder.Services.AddTransient<BookingDetailPage>();
		builder.Services.AddTransient<AddPetPage>();
		builder.Services.AddTransient<ChatListPage>();
		builder.Services.AddTransient<ChatPage>();
		builder.Services.AddTransient<CalendarBookingPage>();
		builder.Services.AddTransient<PetCareRequestDetailPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
