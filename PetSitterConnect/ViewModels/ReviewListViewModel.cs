using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetSitterConnect.Interfaces;
using PetSitterConnect.Models;
using System.Collections.ObjectModel;

namespace PetSitterConnect.ViewModels;

[QueryProperty(nameof(UserId), "UserId")]
public partial class ReviewListViewModel : BaseViewModel
{
    private readonly IReviewService _reviewService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string userId = string.Empty;

    [ObservableProperty]
    private User? user;

    [ObservableProperty]
    private double averageRating;

    [ObservableProperty]
    private int totalReviews;

    [ObservableProperty]
    private ReviewType? selectedReviewType;

    [ObservableProperty]
    private bool showOwnerReviews = true;

    [ObservableProperty]
    private bool showSitterReviews = true;

    public ObservableCollection<Review> Reviews { get; } = new();
    public ObservableCollection<Review> PendingReviews { get; } = new();

    public ReviewListViewModel(IReviewService reviewService, IAuthService authService)
    {
        _reviewService = reviewService;
        _authService = authService;
        Title = "Reviews";
    }

    partial void OnUserIdChanged(string value)
    {
        Task.Run(async () => await LoadReviewsAsync());
    }

    public override async Task InitializeAsync()
    {
        await LoadReviewsAsync();
    }

    [RelayCommand]
    private async Task LoadReviewsAsync()
    {
        await ExecuteAsync(async () =>
        {
            Reviews.Clear();
            PendingReviews.Clear();

            // If no UserId provided, use current user
            if (string.IsNullOrEmpty(UserId))
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser != null)
                {
                    UserId = currentUser.Id;
                    User = currentUser;
                }
            }

            if (!string.IsNullOrEmpty(UserId))
            {
                // Load user information and reviews
                await LoadUserInfoAsync();
                await LoadUserReviewsAsync();
                await LoadPendingReviewsAsync();
            }
        });
    }

    private async Task LoadUserInfoAsync()
    {
        // In a real implementation, you would load user from UserService
        // For now, we'll simulate this
        AverageRating = await _reviewService.CalculateAverageRatingAsync(UserId);
        TotalReviews = await _reviewService.GetTotalReviewsCountAsync(UserId);
    }

    private async Task LoadUserReviewsAsync()
    {
        var reviewType = GetSelectedReviewType();
        var reviews = await _reviewService.GetReviewsForUserAsync(UserId, reviewType);
        
        foreach (var review in reviews)
        {
            Reviews.Add(review);
        }
    }

    private async Task LoadPendingReviewsAsync()
    {
        var currentUser = await _authService.GetCurrentUserAsync();
        if (currentUser != null && currentUser.Id == UserId)
        {
            var pendingReviews = await _reviewService.GetPendingReviewsAsync(UserId);
            foreach (var review in pendingReviews)
            {
                PendingReviews.Add(review);
            }
        }
    }

    [RelayCommand]
    private async Task FilterReviewsAsync(string filterType)
    {
        switch (filterType?.ToLower())
        {
            case "all":
                SelectedReviewType = null;
                ShowOwnerReviews = true;
                ShowSitterReviews = true;
                break;
            case "owner":
                SelectedReviewType = ReviewType.OwnerToSitter;
                ShowOwnerReviews = true;
                ShowSitterReviews = false;
                break;
            case "sitter":
                SelectedReviewType = ReviewType.SitterToOwner;
                ShowOwnerReviews = false;
                ShowSitterReviews = true;
                break;
        }

        await LoadUserReviewsAsync();
    }

    [RelayCommand]
    private async Task WriteReviewAsync(Review pendingReview)
    {
        var parameters = new Dictionary<string, object>
        {
            { "BookingId", pendingReview.BookingId }
        };
        await Shell.Current.GoToAsync("createreview", parameters);
    }

    [RelayCommand]
    private async Task ViewReviewDetailsAsync(Review review)
    {
        var parameters = new Dictionary<string, object>
        {
            { "ReviewId", review.Id }
        };
        await Shell.Current.GoToAsync("reviewdetails", parameters);
    }

    [RelayCommand]
    private async Task RefreshReviewsAsync()
    {
        await LoadReviewsAsync();
    }

    private ReviewType? GetSelectedReviewType()
    {
        if (!ShowOwnerReviews && ShowSitterReviews)
            return ReviewType.SitterToOwner;
        if (ShowOwnerReviews && !ShowSitterReviews)
            return ReviewType.OwnerToSitter;
        return null; // Show all
    }

    public string GetReviewTypeText(ReviewType reviewType)
    {
        return reviewType switch
        {
            ReviewType.OwnerToSitter => "Pet Owner Review",
            ReviewType.SitterToOwner => "Pet Sitter Review",
            _ => "Review"
        };
    }

    public string GetStarRating(int rating)
    {
        return new string('⭐', rating) + new string('☆', 5 - rating);
    }
}