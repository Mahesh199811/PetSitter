using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetSitterConnect.Models;

public class ChatMessage
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(1000)]
    public string Content { get; set; } = string.Empty;

    [Required]
    public MessageType MessageType { get; set; } = MessageType.Text;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }

    public bool IsRead { get; set; } = false;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    [StringLength(255)]
    public string? AttachmentUrl { get; set; }

    [StringLength(100)]
    public string? AttachmentType { get; set; }

    // Foreign keys
    [Required]
    public string SenderId { get; set; } = string.Empty;

    [Required]
    public string ReceiverId { get; set; } = string.Empty;

    [Required]
    public int BookingId { get; set; }

    // Navigation properties
    [ForeignKey("SenderId")]
    public virtual User Sender { get; set; } = null!;

    [ForeignKey("ReceiverId")]
    public virtual User Receiver { get; set; } = null!;

    [ForeignKey("BookingId")]
    public virtual Booking Booking { get; set; } = null!;
}

public enum MessageType
{
    Text = 1,
    Image = 2,
    File = 3,
    System = 4  // System generated messages (booking updates, etc.)
}
