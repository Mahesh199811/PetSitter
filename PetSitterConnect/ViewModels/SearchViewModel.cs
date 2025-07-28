using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetSitterConnect.Interfaces;
using PetSitterConnect.Models;
using System.Collections.ObjectModel;

namespace PetSitterConnect.ViewModels;

public partial class SearchViewModel : BaseViewModel
{
    private readonly ISearchService _searchService;
    private readonly IAuthService _authService;

    public SearchViewModel(ISearchService searchService, IAuthService authService)
    {
        _searchService = searchService;
        _authService = authService;
        Title = "Search";
        
        SearchCriteria = new SearchCriteria();
        SearchResults = new ObservableCollection<PetCareRequestSearchResult>();
        SitterResults = new ObservableCollection<SitterSearchResult>();
        SearchSuggestions = new ObservableCollection<string>();
        SavedSearches = new ObservableCollection<SearchCriteria>();
        
        InitializeFilters();
    }

    [ObservableProperty]
    private SearchCriteria searchCriteria;

    [ObservableProperty]
    private ObservableCollection<PetCareRequestSearchResult> searchResults;

    [ObservableProperty]
    private ObservableCollection<SitterSearchResult> sitterResults;

    [ObservableProperty]
    private ObservableCollection<string> searchSuggestions;

    [ObservableProperty]
    private ObservableCollection<SearchCriteria> savedSearches;

    [ObservableProperty]
    private SearchFilters? availableFilters;

    [ObservableProperty]
    private bool isSearchingRequests = true;

    [ObservableProperty]
    private bool isSearchingSitters = false;

    [ObservableProperty]
    private bool showFilters = false;

    [ObservableProperty]
    private bool hasResults = false;

    [ObservableProperty]
    private bool hasMoreResults = false;

    [ObservableProperty]
    private int totalResults = 0;

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private string selectedLocation = string.Empty;

    [ObservableProperty]
    private decimal minPrice = 0;

    [ObservableProperty]
    private decimal maxPrice = 1000;

    [ObservableProperty]
    private double minRating = 1.0;

    [ObservableProperty]
    private bool emergencyOnly = false;

    [ObservableProperty]
    private bool experiencedOnly = false;

    [ObservableProperty]
    private DateTime? startDate;

    [ObservableProperty]
    private DateTime? endDate;

    [ObservableProperty]
    private SortOption selectedSortOption = SortOption.Relevance;

    public override async Task InitializeAsync()
    {
        await LoadFiltersAsync();
        await LoadSavedSearchesAsync();
        await LoadFeaturedContentAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery) && string.IsNullOrWhiteSpace(SelectedLocation))
        {
            await Shell.Current.DisplayAlert("Search", "Please enter a search term or location", "OK");
            return;
        }

        await ExecuteAsync(async () =>
        {
            UpdateSearchCriteria();

            if (IsSearchingRequests)
            {
                var result = await _searchService.SearchPetCareRequestsAsync(SearchCriteria);
                SearchResults.Clear();
                foreach (var item in result.Items)
                {
                    SearchResults.Add(item);
                }
                TotalResults = result.TotalCount;
                HasMoreResults = result.HasNextPage;
            }
            else
            {
                var result = await _searchService.SearchPetSittersAsync(SearchCriteria);
                SitterResults.Clear();
                foreach (var item in result.Items)
                {
                    SitterResults.Add(item);
                }
                TotalResults = result.TotalCount;
                HasMoreResults = result.HasNextPage;
            }

            HasResults = TotalResults > 0;
        });
    }

    [RelayCommand]
    private async Task LoadMoreResultsAsync()
    {
        if (!HasMoreResults || IsBusy) return;

        await ExecuteAsync(async () =>
        {
            SearchCriteria.PageNumber++;

            if (IsSearchingRequests)
            {
                var result = await _searchService.SearchPetCareRequestsAsync(SearchCriteria);
                foreach (var item in result.Items)
                {
                    SearchResults.Add(item);
                }
                HasMoreResults = result.HasNextPage;
            }
            else
            {
                var result = await _searchService.SearchPetSittersAsync(SearchCriteria);
                foreach (var item in result.Items)
                {
                    SitterResults.Add(item);
                }
                HasMoreResults = result.HasNextPage;
            }
        });
    }

    [RelayCommand]
    private async Task GetSearchSuggestionsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery) || SearchQuery.Length < 2)
        {
            SearchSuggestions.Clear();
            return;
        }

        var suggestions = await _searchService.GetSearchSuggestionsAsync(SearchQuery);
        SearchSuggestions.Clear();
        foreach (var suggestion in suggestions)
        {
            SearchSuggestions.Add(suggestion);
        }
    }

    [RelayCommand]
    private void SelectSuggestion(string suggestion)
    {
        SearchQuery = suggestion;
        SearchSuggestions.Clear();
    }

    [RelayCommand]
    private void ToggleSearchType()
    {
        IsSearchingRequests = !IsSearchingRequests;
        IsSearchingSitters = !IsSearchingSitters;
        
        // Clear previous results
        SearchResults.Clear();
        SitterResults.Clear();
        HasResults = false;
        TotalResults = 0;
    }

    [RelayCommand]
    private void ToggleFilters()
    {
        ShowFilters = !ShowFilters;
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        SearchQuery = string.Empty;
        SelectedLocation = string.Empty;
        MinPrice = 0;
        MaxPrice = 1000;
        MinRating = 1.0;
        EmergencyOnly = false;
        ExperiencedOnly = false;
        StartDate = null;
        EndDate = null;
        SelectedSortOption = SortOption.Relevance;

        SearchCriteria = new SearchCriteria();
        await SearchAsync();
    }

    [RelayCommand]
    private async Task SaveSearchAsync()
    {
        var searchName = await Shell.Current.DisplayPromptAsync("Save Search", 
            "Enter a name for this search:", "Save", "Cancel");

        if (string.IsNullOrWhiteSpace(searchName)) return;

        var currentUser = await _authService.GetCurrentUserAsync();
        if (currentUser != null)
        {
            UpdateSearchCriteria();
            await _searchService.SaveSearchAsync(currentUser.Id, SearchCriteria, searchName);
            await LoadSavedSearchesAsync();
            await Shell.Current.DisplayAlert("Success", "Search saved successfully!", "OK");
        }
    }

    [RelayCommand]
    private async Task LoadSavedSearchAsync(SearchCriteria savedCriteria)
    {
        SearchCriteria = savedCriteria;
        ApplySearchCriteriaToUI();
        await SearchAsync();
    }

    [RelayCommand]
    private async Task ViewRequestDetailsAsync(PetCareRequestSearchResult request)
    {
        var parameters = new Dictionary<string, object>
        {
            { "RequestId", request.Id }
        };
        await Shell.Current.GoToAsync("requestdetails", parameters);
    }

    [RelayCommand]
    private async Task ViewSitterProfileAsync(SitterSearchResult sitter)
    {
        var parameters = new Dictionary<string, object>
        {
            { "SitterId", sitter.Id }
        };
        await Shell.Current.GoToAsync("sitterprofile", parameters);
    }

    [RelayCommand]
    private async Task GetNearbyResultsAsync()
    {
        // This would typically use location services
        // For now, use a default location
        await ExecuteAsync(async () =>
        {
            if (IsSearchingRequests)
            {
                var result = await _searchService.GetNearbyRequestsAsync(0, 0, 10);
                SearchResults.Clear();
                foreach (var item in result.Items)
                {
                    SearchResults.Add(item);
                }
                TotalResults = result.TotalCount;
                HasResults = TotalResults > 0;
            }
            else
            {
                var result = await _searchService.GetNearbySittersAsync(0, 0, 10);
                SitterResults.Clear();
                foreach (var item in result.Items)
                {
                    SitterResults.Add(item);
                }
                TotalResults = result.TotalCount;
                HasResults = TotalResults > 0;
            }
        });
    }

    private async Task LoadFiltersAsync()
    {
        AvailableFilters = await _searchService.GetAvailableFiltersAsync();
        if (AvailableFilters != null)
        {
            MaxPrice = AvailableFilters.MaxPrice;
        }
    }

    private async Task LoadSavedSearchesAsync()
    {
        var currentUser = await _authService.GetCurrentUserAsync();
        if (currentUser != null)
        {
            var searches = await _searchService.GetSavedSearchesAsync(currentUser.Id);
            SavedSearches.Clear();
            foreach (var search in searches)
            {
                SavedSearches.Add(search);
            }
        }
    }

    private async Task LoadFeaturedContentAsync()
    {
        if (IsSearchingRequests)
        {
            var featured = await _searchService.GetFeaturedRequestsAsync(10);
            SearchResults.Clear();
            foreach (var item in featured)
            {
                SearchResults.Add(item);
            }
            HasResults = featured.Any();
        }
        else
        {
            var topRated = await _searchService.GetTopRatedSittersAsync(10);
            SitterResults.Clear();
            foreach (var item in topRated)
            {
                SitterResults.Add(item);
            }
            HasResults = topRated.Any();
        }
    }

    private void UpdateSearchCriteria()
    {
        SearchCriteria.SearchTerm = SearchQuery;
        SearchCriteria.Location = SelectedLocation;
        SearchCriteria.MinPrice = MinPrice > 0 ? MinPrice : null;
        SearchCriteria.MaxPrice = MaxPrice < 1000 ? MaxPrice : null;
        SearchCriteria.MinRating = MinRating > 1.0 ? MinRating : null;
        SearchCriteria.IsEmergencyAvailable = EmergencyOnly ? true : null;
        SearchCriteria.HasExperience = ExperiencedOnly ? true : null;
        SearchCriteria.StartDate = StartDate;
        SearchCriteria.EndDate = EndDate;
        SearchCriteria.SortBy = SelectedSortOption;
        SearchCriteria.PageNumber = 1; // Reset to first page
    }

    private void ApplySearchCriteriaToUI()
    {
        SearchQuery = SearchCriteria.SearchTerm ?? string.Empty;
        SelectedLocation = SearchCriteria.Location ?? string.Empty;
        MinPrice = SearchCriteria.MinPrice ?? 0;
        MaxPrice = SearchCriteria.MaxPrice ?? 1000;
        MinRating = SearchCriteria.MinRating ?? 1.0;
        EmergencyOnly = SearchCriteria.IsEmergencyAvailable ?? false;
        ExperiencedOnly = SearchCriteria.HasExperience ?? false;
        StartDate = SearchCriteria.StartDate;
        EndDate = SearchCriteria.EndDate;
        SelectedSortOption = SearchCriteria.SortBy;
    }

    private async void InitializeFilters()
    {
        await LoadFiltersAsync();
    }

    partial void OnSearchQueryChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Length >= 2)
        {
            Task.Run(async () => await GetSearchSuggestionsAsync());
        }
        else
        {
            SearchSuggestions.Clear();
        }
    }
}