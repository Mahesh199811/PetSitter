using PetSitterConnect.Models;

namespace PetSitterConnect.Interfaces;

public interface ISearchService
{
    // Pet Care Request Search
    Task<SearchResult<PetCareRequestSearchResult>> SearchPetCareRequestsAsync(SearchCriteria criteria);
    Task<SearchResult<PetCareRequestSearchResult>> GetNearbyRequestsAsync(double latitude, double longitude, double radiusKm = 10);
    Task<List<PetCareRequestSearchResult>> GetFeaturedRequestsAsync(int count = 10);
    Task<List<PetCareRequestSearchResult>> GetUrgentRequestsAsync(int count = 5);

    // Pet Sitter Search
    Task<SearchResult<SitterSearchResult>> SearchPetSittersAsync(SearchCriteria criteria);
    Task<SearchResult<SitterSearchResult>> GetNearbySittersAsync(double latitude, double longitude, double radiusKm = 10);
    Task<List<SitterSearchResult>> GetTopRatedSittersAsync(int count = 10);
    Task<List<SitterSearchResult>> GetAvailableSittersAsync(DateTime startDate, DateTime endDate, int count = 20);

    // Search Suggestions and Filters
    Task<List<string>> GetSearchSuggestionsAsync(string query, int maxResults = 10);
    Task<SearchFilters> GetAvailableFiltersAsync();
    Task<List<string>> GetPopularLocationsAsync(int count = 20);
    Task<List<string>> GetPopularSearchTermsAsync(int count = 10);

    // Search History and Saved Searches
    Task SaveSearchAsync(string userId, SearchCriteria criteria, string? searchName = null);
    Task<List<SearchCriteria>> GetSavedSearchesAsync(string userId);
    Task DeleteSavedSearchAsync(string userId, int searchId);
    Task<List<string>> GetSearchHistoryAsync(string userId, int count = 10);

    // Analytics and Insights
    Task RecordSearchAsync(string? userId, SearchCriteria criteria, int resultCount);
    Task<Dictionary<string, int>> GetPopularFiltersAsync();
    Task<Dictionary<ServiceType, int>> GetServiceTypePopularityAsync();
}