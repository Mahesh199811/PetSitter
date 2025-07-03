using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetSitterConnect.Models;

public class Pet
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public PetType Type { get; set; }

    [StringLength(100)]
    public string? Breed { get; set; }

    public int? Age { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; }

    public double? Weight { get; set; }

    [StringLength(50)]
    public string? Size { get; set; } // Small, Medium, Large

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? SpecialNeeds { get; set; }

    [StringLength(500)]
    public string? MedicalConditions { get; set; }

    [StringLength(500)]
    public string? Medications { get; set; }

    [StringLength(500)]
    public string? FeedingInstructions { get; set; }

    [StringLength(255)]
    public string? PhotoUrl { get; set; }

    public bool IsVaccinated { get; set; } = false;
    public bool IsNeutered { get; set; } = false;
    public bool IsFriendlyWithOtherPets { get; set; } = false;
    public bool IsFriendlyWithChildren { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign key
    [Required]
    public string OwnerId { get; set; } = string.Empty;

    // Navigation properties
    [ForeignKey("OwnerId")]
    public virtual User Owner { get; set; } = null!;
    public virtual ICollection<PetCareRequest> CareRequests { get; set; } = new List<PetCareRequest>();
}

public enum PetType
{
    Dog = 1,
    Cat = 2,
    Bird = 3,
    Fish = 4,
    Rabbit = 5,
    Hamster = 6,
    GuineaPig = 7,
    Reptile = 8,
    Other = 9
}
