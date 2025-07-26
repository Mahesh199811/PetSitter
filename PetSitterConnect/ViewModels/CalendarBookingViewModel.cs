using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetSitterConnect.Models;
using PetSitterConnect.Services;
using PetSitterConnect.Interfaces;
using System.Collections.ObjectModel;

namespace PetSitterConnect.ViewModels;

public partial class CalendarBookingViewModel : BaseViewModel
{
    private readonly IPetCareRequestService _petCareRequestService;
    private readonly IBookingService _bookingService;
    private readonly ISchedulingService _schedulingService;
    private readonly IAuthService _authService;

    public CalendarBookingViewModel(
        IPetCareRequestService petCareRequestService,
        IBookingService bookingService,
        ISchedulingService schedulingService,
        IAuthService authService)
    {
        _petCareRequestService = petCareRequestService;
        _bookingService = bookingService;
        _schedulingService = schedulingService;
        _authService = authService;
        Title = "Calendar Booking";

        CalendarDays = new ObservableCollection<CalendarDay>();
        SelectedDates = new ObservableCollection<DateTime>();
        CurrentMonth = DateTime.Today;
        
        GenerateCalendarDays();
    }

    [ObservableProperty]
    private ObservableCollection<CalendarDay> calendarDays;

    [ObservableProperty]
    private ObservableCollection<DateTime> selectedDates;

    [ObservableProperty]
    private DateTime currentMonth;

    [ObservableProperty]
    private DateTime? startDate;

    [ObservableProperty]
    private DateTime? endDate;

    [ObservableProperty]
    private User? currentUser;

    [ObservableProperty]
    private bool isSelectingDateRange;

    [ObservableProperty]
    private string selectionMode = "Start Date";

    [ObservableProperty]
    private string userRoleIcon = "👤";

    [ObservableProperty]
    private string userRoleLabel = "USER";

    [ObservableProperty]
    private Color userRoleColor = Colors.Gray;

    [ObservableProperty]
    private bool showBookingDetails;

    [ObservableProperty]
    private List<Booking> dayBookings = new();

    [ObservableProperty]
    private DateTime selectedDay;

    public override async Task InitializeAsync()
    {
        CurrentUser = await _authService.GetCurrentUserAsync();
        if (CurrentUser != null)
        {
            UpdateUserRoleDisplay();
            await LoadCalendarDataAsync();
        }
    }

    private void UpdateUserRoleDisplay()
    {
        if (CurrentUser == null) return;

        UserRoleIcon = CurrentUser.UserType switch
        {
            UserType.PetOwner => "🏠",
            UserType.PetSitter => "🐕‍🦺",
            UserType.Both => "🏠🐕‍🦺",
            _ => "👤"
        };

        UserRoleLabel = CurrentUser.UserType switch
        {
            UserType.PetOwner => "PET OWNER",
            UserType.PetSitter => "PET SITTER",
            UserType.Both => "OWNER & SITTER",
            _ => "USER"
        };

        UserRoleColor = CurrentUser.UserType switch
        {
            UserType.PetOwner => Color.FromArgb("#2E7D32"), // Green
            UserType.PetSitter => Color.FromArgb("#1976D2"), // Blue
            UserType.Both => Color.FromArgb("#7B1FA2"), // Purple
            _ => Colors.Gray
        };

        Title = CurrentUser.UserType switch
        {
            UserType.PetOwner => "Select Booking Dates",
            UserType.PetSitter => "View Booking Calendar",
            UserType.Both => "Booking Calendar",
            _ => "Calendar"
        };
    }

    [RelayCommand]
    private async Task LoadCalendarDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            if (CurrentUser == null) return;

            // Load confirmed and in-progress bookings for the current month and surrounding dates
            var monthStart = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1).AddDays(-7);
            var monthEnd = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1).AddMonths(1).AddDays(7);

            // Get bookings where user is either sitter or owner
            var sitterBookings = await _bookingService.GetBookingsBySitterAsync(CurrentUser.Id);
            var ownerBookings = await _bookingService.GetBookingsByOwnerAsync(CurrentUser.Id);
            var allUserBookings = sitterBookings.Concat(ownerBookings).Distinct();

            var relevantBookings = allUserBookings.Where(b =>
                (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress || b.Status == BookingStatus.Completed) &&
                b.PetCareRequest.StartDate.Date <= monthEnd &&
                b.PetCareRequest.EndDate.Date >= monthStart).ToList();

            // Update calendar days with booking information
            foreach (var day in CalendarDays)
            {
                var dayBookings = relevantBookings.Where(b =>
                    b.PetCareRequest.StartDate.Date <= day.Date &&
                    b.PetCareRequest.EndDate.Date >= day.Date).ToList();

                day.HasBookings = dayBookings.Any();
                day.BookingCount = dayBookings.Count();
                day.BookingStatus = dayBookings.FirstOrDefault()?.Status ?? BookingStatus.Pending;

                // Mark if this day is part of a confirmed booking range
                day.IsBookingDay = dayBookings.Any(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress);
                day.IsBookingStart = dayBookings.Any(b => b.PetCareRequest.StartDate.Date == day.Date);
                day.IsBookingEnd = dayBookings.Any(b => b.PetCareRequest.EndDate.Date == day.Date);
            }
        });
    }

    [RelayCommand]
    private void GenerateCalendarDays()
    {
        CalendarDays.Clear();

        var firstDayOfMonth = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
        var startDate = firstDayOfMonth.AddDays(-(int)firstDayOfMonth.DayOfWeek);

        for (int i = 0; i < 42; i++) // 6 weeks * 7 days
        {
            var date = startDate.AddDays(i);
            var calendarDay = new CalendarDay
            {
                Date = date,
                IsCurrentMonth = date.Month == CurrentMonth.Month,
                IsToday = date.Date == DateTime.Today,
                IsSelected = SelectedDates.Contains(date.Date),
                IsAvailable = date >= DateTime.Today,
                DayNumber = date.Day
            };

            CalendarDays.Add(calendarDay);
        }
    }

    [RelayCommand]
    private async Task SelectDateAsync(CalendarDay day)
    {
        if (!day.IsAvailable || !day.IsCurrentMonth) return;

        if (CurrentUser?.UserType == UserType.PetOwner || CurrentUser?.UserType == UserType.Both)
        {
            await HandleOwnerDateSelectionAsync(day);
        }
        else if (CurrentUser?.UserType == UserType.PetSitter)
        {
            await HandleSitterDateSelectionAsync(day);
        }
    }

    private async Task HandleOwnerDateSelectionAsync(CalendarDay day)
    {
        if (!IsSelectingDateRange)
        {
            // Start date range selection
            StartDate = day.Date;
            EndDate = null;
            IsSelectingDateRange = true;
            SelectionMode = "End Date";
            
            // Clear previous selections
            foreach (var calDay in CalendarDays)
            {
                calDay.IsSelected = false;
                calDay.IsInRange = false;
            }
            
            day.IsSelected = true;
        }
        else
        {
            // Complete date range selection
            if (day.Date < StartDate)
            {
                // Swap dates if end is before start
                EndDate = StartDate;
                StartDate = day.Date;
            }
            else
            {
                EndDate = day.Date;
            }

            IsSelectingDateRange = false;
            SelectionMode = "Start Date";

            // Update visual selection
            UpdateDateRangeSelection();

            // Navigate to create booking page
            await NavigateToCreateBookingAsync();
        }
    }

    private async Task HandleSitterDateSelectionAsync(CalendarDay day)
    {
        SelectedDay = day.Date;

        // Load bookings for selected day (including confirmed, in-progress, and completed)
        if (CurrentUser != null)
        {
            // Get bookings where user is either sitter or owner
            var sitterBookings = await _bookingService.GetBookingsBySitterAsync(CurrentUser.Id);
            var ownerBookings = await _bookingService.GetBookingsByOwnerAsync(CurrentUser.Id);
            var allUserBookings = sitterBookings.Concat(ownerBookings).Distinct();

            var dayBookings = allUserBookings.Where(b =>
                (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress || b.Status == BookingStatus.Completed) &&
                b.PetCareRequest.StartDate.Date <= day.Date &&
                b.PetCareRequest.EndDate.Date >= day.Date).ToList();

            DayBookings = dayBookings;
            ShowBookingDetails = DayBookings.Any();
        }

        // Update selection visual
        foreach (var calDay in CalendarDays)
        {
            calDay.IsSelected = calDay.Date.Date == day.Date.Date;
        }
    }

    private void UpdateDateRangeSelection()
    {
        if (StartDate == null || EndDate == null) return;

        foreach (var day in CalendarDays)
        {
            day.IsSelected = day.Date.Date == StartDate.Value.Date || day.Date.Date == EndDate.Value.Date;
            day.IsInRange = day.Date.Date > StartDate.Value.Date && day.Date.Date < EndDate.Value.Date;
        }
    }

    [RelayCommand]
    private async Task NavigateToCreateBookingAsync()
    {
        if (StartDate == null || EndDate == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "StartDate", StartDate.Value },
            { "EndDate", EndDate.Value }
        };

        await Shell.Current.GoToAsync("createrequest", navigationParameter);
    }

    [RelayCommand]
    private async Task ViewBookingDetailsAsync(Booking booking)
    {
        if (booking == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "BookingId", booking.Id }
        };

        await Shell.Current.GoToAsync("bookingdetails", navigationParameter);
    }

    [RelayCommand]
    private void PreviousMonth()
    {
        CurrentMonth = CurrentMonth.AddMonths(-1);
        GenerateCalendarDays();
        Task.Run(async () => await LoadCalendarDataAsync());
    }

    [RelayCommand]
    private void NextMonth()
    {
        CurrentMonth = CurrentMonth.AddMonths(1);
        GenerateCalendarDays();
        Task.Run(async () => await LoadCalendarDataAsync());
    }

    [RelayCommand]
    private void ClearSelection()
    {
        StartDate = null;
        EndDate = null;
        IsSelectingDateRange = false;
        SelectionMode = "Start Date";
        ShowBookingDetails = false;

        foreach (var day in CalendarDays)
        {
            day.IsSelected = false;
            day.IsInRange = false;
        }
    }

    partial void OnCurrentMonthChanged(DateTime value)
    {
        GenerateCalendarDays();
        Task.Run(async () => await LoadCalendarDataAsync());
    }
}

public partial class CalendarDay : ObservableObject
{
    [ObservableProperty]
    private DateTime date;

    [ObservableProperty]
    private int dayNumber;

    [ObservableProperty]
    private bool isCurrentMonth;

    [ObservableProperty]
    private bool isToday;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isInRange;

    [ObservableProperty]
    private bool isAvailable;

    [ObservableProperty]
    private bool hasBookings;

    [ObservableProperty]
    private int bookingCount;

    [ObservableProperty]
    private BookingStatus bookingStatus;

    [ObservableProperty]
    private bool isBookingDay;

    [ObservableProperty]
    private bool isBookingStart;

    [ObservableProperty]
    private bool isBookingEnd;

    public Color BackgroundColor
    {
        get
        {
            if (IsSelected) return Color.FromArgb("#007AFF");
            if (IsInRange) return Color.FromArgb("#E3F2FD");
            if (IsToday) return Color.FromArgb("#FFF3E0");
            if (IsBookingDay)
            {
                // Different colors for different booking statuses
                return BookingStatus switch
                {
                    BookingStatus.Confirmed => Color.FromArgb("#C8E6C9"), // Light green for confirmed
                    BookingStatus.InProgress => Color.FromArgb("#BBDEFB"), // Light blue for in progress
                    BookingStatus.Completed => Color.FromArgb("#D1C4E9"), // Light purple for completed
                    _ => Color.FromArgb("#E8F5E8") // Default light green
                };
            }
            if (HasBookings) return Color.FromArgb("#E8F5E8");
            if (!IsCurrentMonth) return Color.FromArgb("#F5F5F5");
            return Colors.Transparent;
        }
    }

    public Color TextColor
    {
        get
        {
            if (IsSelected) return Colors.White;
            if (!IsCurrentMonth) return Color.FromArgb("#BDBDBD");
            if (!IsAvailable) return Color.FromArgb("#BDBDBD");
            if (IsBookingDay)
            {
                // Darker text for booking days to ensure readability
                return BookingStatus switch
                {
                    BookingStatus.Confirmed => Color.FromArgb("#2E7D32"), // Dark green
                    BookingStatus.InProgress => Color.FromArgb("#1976D2"), // Dark blue
                    BookingStatus.Completed => Color.FromArgb("#7B1FA2"), // Dark purple
                    _ => Color.FromArgb("#388E3C") // Default dark green
                };
            }
            return Color.FromArgb("#212121");
        }
    }
}
