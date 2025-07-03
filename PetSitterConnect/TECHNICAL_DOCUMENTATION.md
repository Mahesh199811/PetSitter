# PetSitter Connect - Technical Documentation

## 🏗️ Architecture Overview

PetSitter Connect follows a clean architecture pattern with clear separation of concerns:

### **Layer Architecture**

```
┌─────────────────────────────────────┐
│           Presentation Layer        │
│  (Views, ViewModels, Converters)    │
├─────────────────────────────────────┤
│          Business Logic Layer       │
│     (Services, Domain Logic)        │
├─────────────────────────────────────┤
│            Data Layer               │
│  (DbContext, Repositories, Models)  │
├─────────────────────────────────────┤
│          Infrastructure Layer       │
│   (Database, External APIs, etc.)   │
└─────────────────────────────────────┘
```

## 🔧 Implementation Details

### **MVVM Pattern Implementation**

**BaseViewModel.cs** - Foundation for all ViewModels:
```csharp
public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string title = string.Empty;

    protected async Task ExecuteAsync(Func<Task> operation)
    {
        if (IsBusy) return;
        
        try
        {
            IsBusy = true;
            await operation();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

**Key Features:**
- **Source Generators**: Uses CommunityToolkit.Mvvm source generators for property and command generation
- **Error Handling**: Centralized error handling with user-friendly messages
- **Loading States**: Built-in busy state management
- **Command Pattern**: RelayCommand for user interactions

### **Dependency Injection Setup**

**MauiProgram.cs** - Service Registration:
```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        
        // Configure Entity Framework
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "petsitter.db");
        builder.Services.AddDbContext<PetSitterDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Configure Identity
        builder.Services.AddIdentityCore<User>()
            .AddEntityFrameworkStores<PetSitterDbContext>();

        // Register Services
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IBookingService, BookingService>();
        builder.Services.AddScoped<ISchedulingService, SchedulingService>();
        builder.Services.AddScoped<INotificationService, NotificationService>();

        // Register ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<BookingListViewModel>();
        
        return builder.Build();
    }
}
```

### **Database Configuration**

**Entity Framework Setup:**
```csharp
public class PetSitterDbContext : IdentityDbContext<User>
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure relationships
        builder.Entity<Booking>(entity =>
        {
            entity.HasOne(e => e.PetCareRequest)
                  .WithMany(e => e.Bookings)
                  .HasForeignKey(e => e.PetCareRequestId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Sitter)
                  .WithMany(e => e.BookingsAsSitter)
                  .HasForeignKey(e => e.SitterId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure indexes for performance
        builder.Entity<Booking>()
            .HasIndex(e => e.Status);
        builder.Entity<PetCareRequest>()
            .HasIndex(e => new { e.StartDate, e.EndDate });
    }
}
```

### **Navigation Implementation**

**Shell-based Navigation:**
```csharp
// AppShell.xaml.cs
public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        // Register modal routes
        Routing.RegisterRoute("bookingdetails", typeof(BookingDetailPage));
        Routing.RegisterRoute("createrequest", typeof(CreatePetCareRequestPage));
    }
}

// ViewModel Navigation
[RelayCommand]
private async Task ViewBookingDetailsAsync(Booking booking)
{
    var parameters = new Dictionary<string, object>
    {
        { "BookingId", booking.Id }
    };
    await Shell.Current.GoToAsync("bookingdetails", parameters);
}

// Page Parameter Handling
[QueryProperty(nameof(BookingId), "BookingId")]
public partial class BookingDetailViewModel : BaseViewModel
{
    [ObservableProperty]
    private int bookingId;

    partial void OnBookingIdChanged(int value)
    {
        if (value > 0)
        {
            Task.Run(async () => await LoadBookingDetailsAsync());
        }
    }
}
```

## 🔄 Business Logic Implementation

### **Booking Workflow State Machine**

```csharp
public enum BookingStatus
{
    Pending = 1,        // Initial state when sitter applies
    Confirmed = 2,      // Owner accepts application
    InProgress = 3,     // Service is currently active
    Completed = 4,      // Service completed successfully
    Cancelled = 5,      // Cancelled by either party
    Rejected = 6        // Owner rejects application
}

// State transition validation
public bool CanTransitionTo(BookingStatus newStatus)
{
    return Status switch
    {
        BookingStatus.Pending => newStatus is BookingStatus.Confirmed or BookingStatus.Rejected or BookingStatus.Cancelled,
        BookingStatus.Confirmed => newStatus is BookingStatus.InProgress or BookingStatus.Cancelled,
        BookingStatus.InProgress => newStatus is BookingStatus.Completed or BookingStatus.Cancelled,
        _ => false
    };
}
```

### **Scheduling Conflict Detection**

```csharp
public async Task<bool> HasConflictAsync(string sitterId, DateTime startDate, DateTime endDate, int? excludeBookingId = null)
{
    var query = _context.Bookings
        .Include(b => b.PetCareRequest)
        .Where(b => b.SitterId == sitterId &&
                   (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress) &&
                   b.PetCareRequest.StartDate < endDate &&
                   b.PetCareRequest.EndDate > startDate);

    if (excludeBookingId.HasValue)
    {
        query = query.Where(b => b.Id != excludeBookingId.Value);
    }

    return await query.AnyAsync();
}
```

### **Data Validation & Business Rules**

```csharp
// Model validation attributes
public class PetCareRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Budget must be greater than 0")]
    public decimal Budget { get; set; }

    // Business rule validation
    public bool IsValid()
    {
        return StartDate >= DateTime.Today && 
               EndDate > StartDate && 
               Budget > 0 &&
               !string.IsNullOrWhiteSpace(Title);
    }
}

// ViewModel validation
private bool ValidateInput()
{
    if (StartDate < DateTime.Today)
    {
        ErrorMessage = "Start date cannot be in the past";
        return false;
    }

    if (EndDate <= StartDate)
    {
        ErrorMessage = "End date must be after start date";
        return false;
    }

    return true;
}
```

## 🎨 UI Implementation

### **Data Binding with Converters**

```csharp
// Status to Color Converter
public class StatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is BookingStatus status)
        {
            return status switch
            {
                BookingStatus.Pending => Color.FromArgb("#FF9500"),
                BookingStatus.Confirmed => Color.FromArgb("#007AFF"),
                BookingStatus.InProgress => Color.FromArgb("#34C759"),
                BookingStatus.Completed => Color.FromArgb("#30D158"),
                BookingStatus.Cancelled => Color.FromArgb("#FF3B30"),
                _ => Color.FromArgb("#8E8E93")
            };
        }
        return Color.FromArgb("#8E8E93");
    }
}

// XAML Usage
<Label Text="{Binding Status}"
       TextColor="White"
       BackgroundColor="{Binding Status, Converter={StaticResource StatusToColorConverter}}" />
```

### **Responsive UI Design**

```xml
<!-- Adaptive layout for different screen sizes -->
<Grid ColumnDefinitions="*,Auto" RowDefinitions="Auto,Auto,Auto">
    
    <!-- Mobile layout -->
    <StackLayout Grid.Row="0" Grid.ColumnSpan="2" 
                 IsVisible="{OnIdiom Phone=True, Tablet=False}">
        <!-- Vertical layout for phones -->
    </StackLayout>
    
    <!-- Tablet layout -->
    <StackLayout Grid.Row="0" Grid.Column="0"
                 IsVisible="{OnIdiom Phone=False, Tablet=True}">
        <!-- Side-by-side layout for tablets -->
    </StackLayout>
    
</Grid>
```

## 🔒 Security Implementation

### **Authentication & Authorization**

```csharp
// Session management
public async Task<User?> GetCurrentUserAsync()
{
    var userId = Preferences.Get("CurrentUserId", string.Empty);
    if (!string.IsNullOrEmpty(userId))
    {
        return await _userManager.FindByIdAsync(userId);
    }
    return null;
}

// Authorization checks
public async Task<bool> CanModifyBookingAsync(int bookingId, string userId)
{
    var booking = await _context.Bookings.FindAsync(bookingId);
    return booking != null && (booking.OwnerId == userId || booking.SitterId == userId);
}
```

### **Data Protection**

```csharp
// Sensitive data handling
public class User : IdentityUser
{
    // Personal information
    [PersonalData]
    public string FirstName { get; set; } = string.Empty;
    
    [PersonalData]
    public string LastName { get; set; } = string.Empty;
    
    // Location data (anonymized for privacy)
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
```

## 📊 Performance Optimization

### **Database Query Optimization**

```csharp
// Efficient loading with includes
public async Task<IEnumerable<Booking>> GetBookingsWithDetailsAsync(string userId)
{
    return await _context.Bookings
        .Include(b => b.PetCareRequest)
            .ThenInclude(r => r.Pet)
        .Include(b => b.Sitter)
        .Include(b => b.Owner)
        .Where(b => b.SitterId == userId || b.OwnerId == userId)
        .OrderByDescending(b => b.CreatedAt)
        .ToListAsync();
}

// Pagination for large datasets
public async Task<PagedResult<PetCareRequest>> GetRequestsPagedAsync(int page, int pageSize)
{
    var query = _context.PetCareRequests
        .Where(r => r.Status == RequestStatus.Open)
        .OrderByDescending(r => r.CreatedAt);

    var totalCount = await query.CountAsync();
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new PagedResult<PetCareRequest>
    {
        Items = items,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}
```

### **Memory Management**

```csharp
// Proper disposal in ViewModels
public partial class BookingListViewModel : BaseViewModel, IDisposable
{
    private CancellationTokenSource? _cancellationTokenSource;

    protected override void OnDisappearing()
    {
        _cancellationTokenSource?.Cancel();
        base.OnDisappearing();
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
    }
}
```

## 🧪 Testing Strategy

### **Unit Testing Setup**

```csharp
[TestClass]
public class BookingServiceTests
{
    private PetSitterDbContext _context;
    private BookingService _bookingService;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<PetSitterDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PetSitterDbContext(options);
        _bookingService = new BookingService(_context, Mock.Of<INotificationService>(), Mock.Of<ISchedulingService>());
    }

    [TestMethod]
    public async Task AcceptBooking_ValidBooking_ReturnsTrue()
    {
        // Arrange
        var booking = new Booking { Status = BookingStatus.Pending };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        // Act
        var result = await _bookingService.AcceptBookingAsync(booking.Id);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(BookingStatus.Confirmed, booking.Status);
    }
}
```

This technical documentation provides a comprehensive overview of the implementation details, architectural decisions, and best practices used in the PetSitter Connect application.
