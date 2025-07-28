using System.ComponentModel.DataAnnotations;

namespace PetSitterConnect.Models;

public class SearchCriteria
{
    public string? SearchTerm { get; set; }
    public string? Location { get; set; }
    public double? MaxDistance { get; set; } // in kilometers
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public double? MinRating { get; set; }
    public List<ServiceType> ServiceTypes { get; set; } = new();
    public List<PetType> PetTypes { get; set; } = new();
    public List<string> Amenities { get; set; } = new();
    public bool? IsEmergencyAvailable { get; set; }
    public bool? HasExperience { get; set; }
    public SortOption SortBy { get; set; } = SortOption.Relevance;
    public bool SortDescending { get; set; } = false;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class SearchFilters
{
    public List<ServiceType> AvailableServiceTypes { get; set; } = new();
    public List<PetType> AvailablePetTypes { get; set; } = new();
    public List<string> AvailableAmenities { get; set; } = new();
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public double MinRating { get; set; } = 1.0;
    public double MaxRating { get; set; } = 5.0;
    public List<string> AvailableLocations { get; set; } = new();
}

public class SearchResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
    public SearchFilters? AppliedFilters { get; set; }
}

public class PetCareRequestSearchResult
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal PricePerDay { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ServiceType ServiceType { get; set; }
    public PetType PetType { get; set; }
    public string PetName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public double OwnerRating { get; set; }
    public int OwnerReviewCount { get; set; }
    public List<string> RequiredSkills { get; set; } = new();
    public bool IsUrgent { get; set; }
    public double? Distance { get; set; } // in kilometers
    public DateTime CreatedAt { get; set; }
    public string? ImageUrl { get; set; }
}

public class SitterSearchResult
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public List<ServiceType> ServiceTypes { get; set; } = new();
    public List<PetType> PetTypes { get; set; } = new();
    public List<string> Skills { get; set; } = new();
    public bool IsAvailable { get; set; }
    public bool IsEmergencyAvailable { get; set; }
    public int YearsOfExperience { get; set; }
    public double? Distance { get; set; } // in kilometers
    public string? ProfileImageUrl { get; set; }
    public DateTime LastActive { get; set; }
}

public enum SortOption
{
    Relevance,
    Price,
    Rating,
    Distance,
    Date,
    Popularity,
    Newest
}

public enum ServiceType
{
    PetSitting,
    DogWalking,
    PetBoarding,
    PetGrooming,
    VeterinaryVisit,
    PetTraining,
    PetTaxi,
    Overnight
}

