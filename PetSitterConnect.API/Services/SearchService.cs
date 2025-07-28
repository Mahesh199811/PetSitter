using Microsoft.EntityFrameworkCore;
using PetSitterConnect.Data;
using PetSitterConnect.Interfaces;
using PetSitterConnect.Models;
using System.Text.Json;

namespace PetSitterConnect.Services;

public class SearchService : ISearchService
{
    private readonly PetSitterDbContext _context;
    private readonly IAuthService _authService;

    public SearchService(PetSitterDbContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    public async Task<SearchResult<PetCareRequestSearchResult>> SearchPetCareRequestsAsync(SearchCriteria criteria)
    {
        var query = _context.PetCareRequests
            .Include(r => r.Pet)
            .Include(r => r.Owner)
            .Where(r => r.Status == RequestStatus.Open);

        // Apply filters
        query = ApplyPetCareRequestFilters(query, criteria);

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = ApplySorting(query, criteria.SortBy, criteria.SortDescending);

        // Apply pagination
        var items = await query
            .Skip((criteria.PageNumber - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(r => new PetCareRequestSearchResult
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                Location = r.Location,
                PricePerDay = r.Budget,
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                ServiceType = MapToServiceType(r.CareType.ToString()),
                PetType = r.Pet.Type,
                PetName = r.Pet.Name,
                OwnerName = r.Owner.FullName,
                OwnerRating = r.Owner.AverageRating,
                OwnerReviewCount = r.Owner.TotalReviews,
                RequiredSkills = new List<string>(), // Will be populated from special instructions
                IsUrgent = false, // Will be determined by date proximity
                CreatedAt = r.CreatedAt,
                ImageUrl = r.Pet.PhotoUrl
            })
            .ToListAsync();

        // Record search analytics
        var currentUser = await _authService.GetCurrentUserAsync();
        await RecordSearchAsync(currentUser?.Id, criteria, totalCount);

        return new SearchResult<PetCareRequestSearchResult>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = criteria.PageNumber,
            PageSize = criteria.PageSize,
            AppliedFilters = await GetAvailableFiltersAsync()
        };
    }

    public async Task<SearchResult<SitterSearchResult>> SearchPetSittersAsync(SearchCriteria criteria)
    {
        var query = _context.Users
            .Where(u => u.UserType == UserType.PetSitter || u.UserType == UserType.Both)
            .Where(u => u.IsActive);

        // Apply filters
        query = ApplySitterFilters(query, criteria);

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = ApplySitterSorting(query, criteria.SortBy, criteria.SortDescending);

        // Apply pagination
        var items = await query
            .Skip((criteria.PageNumber - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(u => new SitterSearchResult
            {
                Id = u.Id,
                FullName = u.FullName,
                Location = $"{u.City}, {u.Country}".Trim(' ', ','),
                Bio = u.Bio ?? string.Empty,
                HourlyRate = u.HourlyRate ?? 0,
                AverageRating = u.AverageRating,
                TotalReviews = u.TotalReviews,
                ServiceTypes = GetUserServiceTypes(u),
                PetTypes = GetUserPetTypes(u),
                Skills = GetUserSkills(u),
                IsAvailable = u.IsAvailable,
                IsEmergencyAvailable = false, // Will be determined by availability
                YearsOfExperience = CalculateExperience(u.DateJoined),
                ProfileImageUrl = u.ProfileImageUrl,
                LastActive = u.LastActive
            })
            .ToListAsync();

        // Record search analytics
        var currentUser = await _authService.GetCurrentUserAsync();
        await RecordSearchAsync(currentUser?.Id, criteria, totalCount);

        return new SearchResult<SitterSearchResult>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = criteria.PageNumber,
            PageSize = criteria.PageSize,
            AppliedFilters = await GetAvailableFiltersAsync()
        };
    }

    public async Task<SearchResult<PetCareRequestSearchResult>> GetNearbyRequestsAsync(double latitude, double longitude, double radiusKm = 10)
    {
        // For simplicity, using basic location filtering
        // In production, you'd use spatial queries or external geocoding services
        var criteria = new SearchCriteria
        {
            MaxDistance = radiusKm,
            SortBy = SortOption.Distance,
            PageSize = 20
        };

        return await SearchPetCareRequestsAsync(criteria);
    }

    public async Task<SearchResult<SitterSearchResult>> GetNearbySittersAsync(double latitude, double longitude, double radiusKm = 10)
    {
        var criteria = new SearchCriteria
        {
            MaxDistance = radiusKm,
            SortBy = SortOption.Distance,
            PageSize = 20
        };

        return await SearchPetSittersAsync(criteria);
    }

    public async Task<List<PetCareRequestSearchResult>> GetFeaturedRequestsAsync(int count = 10)
    {
        var result = await SearchPetCareRequestsAsync(new SearchCriteria
        {
            SortBy = SortOption.Popularity,
            PageSize = count
        });

        return result.Items;
    }

    public async Task<List<PetCareRequestSearchResult>> GetUrgentRequestsAsync(int count = 5)
    {
        var result = await SearchPetCareRequestsAsync(new SearchCriteria
        {
            IsEmergencyAvailable = true,
            SortBy = SortOption.Date,
            SortDescending = true,
            PageSize = count
        });

        return result.Items;
    }

    public async Task<List<SitterSearchResult>> GetTopRatedSittersAsync(int count = 10)
    {
        var result = await SearchPetSittersAsync(new SearchCriteria
        {
            MinRating = 4.0,
            SortBy = SortOption.Rating,
            SortDescending = true,
            PageSize = count
        });

        return result.Items;
    }

    public async Task<List<SitterSearchResult>> GetAvailableSittersAsync(DateTime startDate, DateTime endDate, int count = 20)
    {
        var result = await SearchPetSittersAsync(new SearchCriteria
        {
            StartDate = startDate,
            EndDate = endDate,
            SortBy = SortOption.Rating,
            SortDescending = true,
            PageSize = count
        });

        return result.Items;
    }

    public async Task<List<string>> GetSearchSuggestionsAsync(string query, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new List<string>();

        var suggestions = new List<string>();

        // Location suggestions
        var locations = await _context.PetCareRequests
            .Where(r => r.Location.Contains(query))
            .Select(r => r.Location)
            .Distinct()
            .Take(maxResults / 2)
            .ToListAsync();
        suggestions.AddRange(locations);

        // Service type suggestions
        var serviceTypes = Enum.GetNames<ServiceType>()
            .Where(s => s.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults / 2);
        suggestions.AddRange(serviceTypes);

        return suggestions.Take(maxResults).ToList();
    }

    public async Task<SearchFilters> GetAvailableFiltersAsync()
    {
        var filters = new SearchFilters();

        // Get available service types
        filters.AvailableServiceTypes = Enum.GetValues<ServiceType>().ToList();

        // Get available pet types
        filters.AvailablePetTypes = Enum.GetValues<PetType>().ToList();

        // Get price range - use LINQ to Objects to avoid SQLite decimal aggregate issues
        var budgets = await _context.PetCareRequests
            .Where(r => r.Status == RequestStatus.Open)
            .Select(r => r.Budget)
            .ToListAsync();

        if (budgets.Any())
        {
            filters.MinPrice = budgets.Min();
            filters.MaxPrice = budgets.Max();
        }

        // Get available locations
        filters.AvailableLocations = await _context.PetCareRequests
            .Where(r => r.Status == RequestStatus.Open)
            .Select(r => r.Location)
            .Distinct()
            .OrderBy(l => l)
            .Take(50)
            .ToListAsync();

        // Get common amenities/skills from experience descriptions
        filters.AvailableAmenities = await _context.Users
            .Where(u => u.UserType == UserType.PetSitter || u.UserType == UserType.Both)
            .Where(u => u.Experience != null)
            .Select(u => u.Experience!)
            .ToListAsync()
            .ContinueWith(task => 
            {
                var experiences = task.Result;
                var commonSkills = new List<string> 
                { 
                    "Dog Walking", "Pet Sitting", "Feeding", "Grooming", 
                    "Medication Administration", "Emergency Care", "Training",
                    "Overnight Care", "Pet Transportation", "Exercise"
                };
                return commonSkills;
            });

        return filters;
    }

    public async Task<List<string>> GetPopularLocationsAsync(int count = 20)
    {
        return await _context.PetCareRequests
            .Where(r => r.Status == RequestStatus.Open)
            .GroupBy(r => r.Location)
            .OrderByDescending(g => g.Count())
            .Take(count)
            .Select(g => g.Key)
            .ToListAsync();
    }

    public async Task<List<string>> GetPopularSearchTermsAsync(int count = 10)
    {
        // This would typically come from search analytics
        // For now, return common service types
        return Enum.GetNames<ServiceType>()
            .Take(count)
            .ToList();
    }

    public async Task SaveSearchAsync(string userId, SearchCriteria criteria, string? searchName = null)
    {
        var savedSearch = new SavedSearch
        {
            UserId = userId,
            Name = searchName ?? $"Search {DateTime.Now:yyyy-MM-dd HH:mm}",
            Criteria = JsonSerializer.Serialize(criteria),
            CreatedAt = DateTime.UtcNow
        };

        _context.SavedSearches.Add(savedSearch);
        await _context.SaveChangesAsync();
    }

    public async Task<List<SearchCriteria>> GetSavedSearchesAsync(string userId)
    {
        var savedSearches = await _context.SavedSearches
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return savedSearches
            .Select(s => JsonSerializer.Deserialize<SearchCriteria>(s.Criteria))
            .Where(c => c != null)
            .Cast<SearchCriteria>()
            .ToList();
    }

    public async Task DeleteSavedSearchAsync(string userId, int searchId)
    {
        var savedSearch = await _context.SavedSearches
            .FirstOrDefaultAsync(s => s.Id == searchId && s.UserId == userId);

        if (savedSearch != null)
        {
            _context.SavedSearches.Remove(savedSearch);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<string>> GetSearchHistoryAsync(string userId, int count = 10)
    {
        return await _context.SearchAnalytics
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SearchedAt)
            .Select(s => s.SearchTerm ?? "")
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct()
            .Take(count)
            .ToListAsync();
    }

    public async Task RecordSearchAsync(string? userId, SearchCriteria criteria, int resultCount)
    {
        var analytics = new SearchAnalytics
        {
            UserId = userId,
            SearchTerm = criteria.SearchTerm,
            Location = criteria.Location,
            ServiceType = criteria.ServiceTypes.FirstOrDefault().ToString(),
            ResultCount = resultCount,
            SearchedAt = DateTime.UtcNow
        };

        _context.SearchAnalytics.Add(analytics);
        await _context.SaveChangesAsync();
    }

    public async Task<Dictionary<string, int>> GetPopularFiltersAsync()
    {
        return await _context.SearchAnalytics
            .Where(s => s.SearchedAt >= DateTime.UtcNow.AddDays(-30))
            .GroupBy(s => s.ServiceType)
            .ToDictionaryAsync(g => g.Key ?? "Unknown", g => g.Count());
    }

    public async Task<Dictionary<ServiceType, int>> GetServiceTypePopularityAsync()
    {
        var stats = await _context.SearchAnalytics
            .Where(s => s.SearchedAt >= DateTime.UtcNow.AddDays(-30))
            .GroupBy(s => s.ServiceType)
            .ToDictionaryAsync(g => g.Key ?? "Unknown", g => g.Count());

        var result = new Dictionary<ServiceType, int>();
        foreach (var stat in stats)
        {
            if (Enum.TryParse<ServiceType>(stat.Key, out var serviceType))
            {
                result[serviceType] = stat.Value;
            }
        }

        return result;
    }

    // Helper methods
    private IQueryable<PetCareRequest> ApplyPetCareRequestFilters(IQueryable<PetCareRequest> query, SearchCriteria criteria)
    {
        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            query = query.Where(r => r.Title.Contains(criteria.SearchTerm) ||
                                   r.Description.Contains(criteria.SearchTerm) ||
                                   r.Pet.Name.Contains(criteria.SearchTerm));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Location))
        {
            query = query.Where(r => r.Location.Contains(criteria.Location));
        }

        if (criteria.StartDate.HasValue)
        {
            query = query.Where(r => r.StartDate >= criteria.StartDate.Value);
        }

        if (criteria.EndDate.HasValue)
        {
            query = query.Where(r => r.EndDate <= criteria.EndDate.Value);
        }

        if (criteria.MinPrice.HasValue)
        {
            query = query.Where(r => r.Budget >= criteria.MinPrice.Value);
        }

        if (criteria.MaxPrice.HasValue)
        {
            query = query.Where(r => r.Budget <= criteria.MaxPrice.Value);
        }

        if (criteria.IsEmergencyAvailable.HasValue)
        {
            // Consider urgent if start date is within 24 hours
            var urgentDate = DateTime.UtcNow.AddHours(24);
            query = query.Where(r => r.StartDate <= urgentDate);
        }

        return query;
    }

    private IQueryable<User> ApplySitterFilters(IQueryable<User> query, SearchCriteria criteria)
    {
        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            query = query.Where(u => u.FullName.Contains(criteria.SearchTerm) ||
                                   (u.Bio != null && u.Bio.Contains(criteria.SearchTerm)) ||
                                   (u.Experience != null && u.Experience.Contains(criteria.SearchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Location))
        {
            query = query.Where(u => (u.City != null && u.City.Contains(criteria.Location)) ||
                                   (u.Country != null && u.Country.Contains(criteria.Location)));
        }

        if (criteria.MinPrice.HasValue)
        {
            query = query.Where(u => u.HourlyRate != null && u.HourlyRate >= criteria.MinPrice.Value);
        }

        if (criteria.MaxPrice.HasValue)
        {
            query = query.Where(u => u.HourlyRate != null && u.HourlyRate <= criteria.MaxPrice.Value);
        }

        if (criteria.MinRating.HasValue)
        {
            query = query.Where(u => u.AverageRating >= criteria.MinRating.Value);
        }

        if (criteria.IsEmergencyAvailable.HasValue)
        {
            // For now, consider all available users as emergency available
            query = query.Where(u => u.IsAvailable);
        }

        if (criteria.HasExperience.HasValue && criteria.HasExperience.Value)
        {
            query = query.Where(u => u.Experience != null && u.Experience.Length > 0);
        }

        return query;
    }

    private IQueryable<PetCareRequest> ApplySorting(IQueryable<PetCareRequest> query, SortOption sortBy, bool descending)
    {
        return sortBy switch
        {
            SortOption.Price => descending ? query.OrderByDescending(r => r.Budget) : query.OrderBy(r => r.Budget),
            SortOption.Date => descending ? query.OrderByDescending(r => r.StartDate) : query.OrderBy(r => r.StartDate),
            SortOption.Rating => descending ? query.OrderByDescending(r => r.Owner.AverageRating) : query.OrderBy(r => r.Owner.AverageRating),
            SortOption.Newest => query.OrderByDescending(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.CreatedAt) // Default to newest
        };
    }

    private IQueryable<User> ApplySitterSorting(IQueryable<User> query, SortOption sortBy, bool descending)
    {
        return sortBy switch
        {
            SortOption.Price => descending ? query.OrderByDescending(u => u.HourlyRate) : query.OrderBy(u => u.HourlyRate),
            SortOption.Rating => descending ? query.OrderByDescending(u => u.AverageRating) : query.OrderBy(u => u.AverageRating),
            SortOption.Popularity => query.OrderByDescending(u => u.TotalReviews),
            _ => query.OrderByDescending(u => u.AverageRating) // Default to rating
        };
    }

    private ServiceType MapToServiceType(string serviceType)
    {
        return Enum.TryParse<ServiceType>(serviceType, out var result) ? result : ServiceType.PetSitting;
    }

    private PetType MapToPetType(string petType)
    {
        return Enum.TryParse<PetType>(petType, out var result) ? result : PetType.Dog;
    }

    private List<ServiceType> GetUserServiceTypes(User user)
    {
        // This would be based on user's service preferences
        // For now, return common service types
        return new List<ServiceType> { ServiceType.PetSitting, ServiceType.DogWalking };
    }

    private List<PetType> GetUserPetTypes(User user)
    {
        // This would be based on user's pet type preferences
        // For now, return common pet types
        return new List<PetType> { PetType.Dog, PetType.Cat };
    }

    private List<string> GetUserSkills(User user)
    {
        // Extract skills from experience or return default skills
        if (!string.IsNullOrEmpty(user.Experience))
        {
            var commonSkills = new List<string> 
            { 
                "Pet Sitting", "Dog Walking", "Feeding", "Basic Care"
            };
            return commonSkills;
        }
        return new List<string>();
    }

    private int CalculateExperience(DateTime dateJoined)
    {
        var timeSpan = DateTime.UtcNow - dateJoined;
        return Math.Max(0, (int)(timeSpan.TotalDays / 365));
    }
}

