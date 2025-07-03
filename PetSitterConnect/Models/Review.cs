using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetSitterConnect.Models;

public class Review
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }

    [Required]
    public ReviewType ReviewType { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsVisible { get; set; } = true;

    // Foreign keys
    [Required]
    public string ReviewerId { get; set; } = string.Empty; // Person giving the review

    [Required]
    public string RevieweeId { get; set; } = string.Empty; // Person receiving the review

    [Required]
    public int BookingId { get; set; }

    // Navigation properties
    [ForeignKey("ReviewerId")]
    public virtual User Reviewer { get; set; } = null!;

    [ForeignKey("RevieweeId")]
    public virtual User Reviewee { get; set; } = null!;

    [ForeignKey("BookingId")]
    public virtual Booking Booking { get; set; } = null!;
}

public enum ReviewType
{
    OwnerToSitter = 1,  // Pet owner reviewing pet sitter
    SitterToOwner = 2   // Pet sitter reviewing pet owner
}
