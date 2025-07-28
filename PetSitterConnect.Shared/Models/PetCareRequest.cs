using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetSitterConnect.Models;

public class PetCareRequest
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public CareType CareType { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Budget { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [StringLength(500)]
    public string? SpecialInstructions { get; set; }

    [Required]
    public RequestStatus Status { get; set; } = RequestStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign keys
    [Required]
    public string OwnerId { get; set; } = string.Empty;

    [Required]
    public int PetId { get; set; }

    // Navigation properties
    [ForeignKey("OwnerId")]
    public virtual User Owner { get; set; } = null!;

    [ForeignKey("PetId")]
    public virtual Pet Pet { get; set; } = null!;

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    // Computed properties
    public int DurationInDays => (EndDate.Date - StartDate.Date).Days + 1;
    public bool IsActive => Status == RequestStatus.Open && EndDate > DateTime.UtcNow;
}

public enum CareType
{
    PetSitting = 1,      // At owner's home
    PetBoarding = 2,     // At sitter's home
    DogWalking = 3,      // Walking service
    Daycare = 4,         // Daily care
    Overnight = 5        // Overnight care
}

public enum RequestStatus
{
    Open = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    Expired = 5
}
