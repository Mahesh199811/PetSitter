using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PetSitterConnect.Models;

public class User : IdentityUser
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Bio { get; set; }

    [StringLength(255)]
    public string? ProfileImageUrl { get; set; }

    [Required]
    public UserType UserType { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(20)]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public DateTime DateJoined { get; set; } = DateTime.UtcNow;
    public DateTime LastActive { get; set; } = DateTime.UtcNow;

    public bool IsVerified { get; set; } = false;
    public bool IsActive { get; set; } = true;

    // Rating properties
    public double AverageRating { get; set; } = 0.0;
    public int TotalReviews { get; set; } = 0;

    // Pet Sitter specific properties
    [StringLength(1000)]
    public string? Experience { get; set; }

    public decimal? HourlyRate { get; set; }
    public bool IsAvailable { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Pet> Pets { get; set; } = new List<Pet>();
    public virtual ICollection<PetCareRequest> PetCareRequests { get; set; } = new List<PetCareRequest>();
    public virtual ICollection<Booking> BookingsAsSitter { get; set; } = new List<Booking>();
    public virtual ICollection<Booking> BookingsAsOwner { get; set; } = new List<Booking>();
    public virtual ICollection<Review> ReviewsGiven { get; set; } = new List<Review>();
    public virtual ICollection<Review> ReviewsReceived { get; set; } = new List<Review>();
    public virtual ICollection<ChatMessage> MessagesSent { get; set; } = new List<ChatMessage>();
    public virtual ICollection<ChatMessage> MessagesReceived { get; set; } = new List<ChatMessage>();

    public string FullName => $"{FirstName} {LastName}";
}

public enum UserType
{
    PetOwner = 1,
    PetSitter = 2,
    Both = 3
}
