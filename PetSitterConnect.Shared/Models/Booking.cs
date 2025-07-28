using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetSitterConnect.Models;

public class Booking
{
    [Key]
    public int Id { get; set; }

    [Required]
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? PlatformFee { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? SitterAmount { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    [StringLength(500)]
    public string? CancellationReason { get; set; }

    // Payment related
    public bool IsPaymentProcessed { get; set; } = false;
    public bool IsPaymentReleased { get; set; } = false;
    public DateTime? PaymentProcessedAt { get; set; }
    public DateTime? PaymentReleasedAt { get; set; }

    [StringLength(100)]
    public string? PaymentTransactionId { get; set; }

    // Foreign keys
    [Required]
    public int PetCareRequestId { get; set; }

    [Required]
    public string SitterId { get; set; } = string.Empty;

    [Required]
    public string OwnerId { get; set; } = string.Empty;

    // Navigation properties
    [ForeignKey("PetCareRequestId")]
    public virtual PetCareRequest PetCareRequest { get; set; } = null!;

    [ForeignKey("SitterId")]
    public virtual User Sitter { get; set; } = null!;

    [ForeignKey("OwnerId")]
    public virtual User Owner { get; set; } = null!;

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    // Computed properties
    public bool CanBeCancelled => Status == BookingStatus.Pending || Status == BookingStatus.Confirmed;
    public bool CanBeCompleted => Status == BookingStatus.InProgress && DateTime.UtcNow >= PetCareRequest.EndDate;
    public bool IsActive => Status == BookingStatus.Confirmed || Status == BookingStatus.InProgress;
}

public enum BookingStatus
{
    Pending = 1,        // Waiting for sitter acceptance
    Confirmed = 2,      // Accepted by sitter, waiting to start
    InProgress = 3,     // Currently active
    Completed = 4,      // Successfully completed
    Cancelled = 5,      // Cancelled by either party
    Rejected = 6        // Rejected by sitter
}
