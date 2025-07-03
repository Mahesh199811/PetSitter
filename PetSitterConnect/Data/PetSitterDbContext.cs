using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PetSitterConnect.Models;

namespace PetSitterConnect.Data;

public class PetSitterDbContext : IdentityDbContext<User>
{
    public PetSitterDbContext(DbContextOptions<PetSitterDbContext> options) : base(options)
    {
    }

    public DbSet<Pet> Pets { get; set; }
    public DbSet<PetCareRequest> PetCareRequests { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure User entity
        builder.Entity<User>(entity =>
        {
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Bio).HasMaxLength(500);
            entity.Property(e => e.ProfileImageUrl).HasMaxLength(255);
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.Experience).HasMaxLength(1000);
            entity.Property(e => e.HourlyRate).HasColumnType("decimal(10,2)");

            entity.HasIndex(e => e.UserType);
            entity.HasIndex(e => new { e.City, e.Country });
            entity.HasIndex(e => e.IsAvailable);
        });

        // Configure Pet entity
        builder.Entity<Pet>(entity =>
        {
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Breed).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.Size).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.SpecialNeeds).HasMaxLength(500);
            entity.Property(e => e.MedicalConditions).HasMaxLength(500);
            entity.Property(e => e.Medications).HasMaxLength(500);
            entity.Property(e => e.FeedingInstructions).HasMaxLength(500);
            entity.Property(e => e.PhotoUrl).HasMaxLength(255);

            entity.HasOne(e => e.Owner)
                  .WithMany(e => e.Pets)
                  .HasForeignKey(e => e.OwnerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.OwnerId);
        });

        // Configure PetCareRequest entity
        builder.Entity<PetCareRequest>(entity =>
        {
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Budget).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.SpecialInstructions).HasMaxLength(500);

            entity.HasOne(e => e.Owner)
                  .WithMany(e => e.PetCareRequests)
                  .HasForeignKey(e => e.OwnerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Pet)
                  .WithMany(e => e.CareRequests)
                  .HasForeignKey(e => e.PetId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CareType);
            entity.HasIndex(e => new { e.StartDate, e.EndDate });
            entity.HasIndex(e => e.OwnerId);
        });

        // Configure Booking entity
        builder.Entity<Booking>(entity =>
        {
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.PlatformFee).HasColumnType("decimal(10,2)");
            entity.Property(e => e.SitterAmount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.CancellationReason).HasMaxLength(500);
            entity.Property(e => e.PaymentTransactionId).HasMaxLength(100);

            entity.HasOne(e => e.PetCareRequest)
                  .WithMany(e => e.Bookings)
                  .HasForeignKey(e => e.PetCareRequestId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Sitter)
                  .WithMany(e => e.BookingsAsSitter)
                  .HasForeignKey(e => e.SitterId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Owner)
                  .WithMany(e => e.BookingsAsOwner)
                  .HasForeignKey(e => e.OwnerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.SitterId);
            entity.HasIndex(e => e.OwnerId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure Review entity
        builder.Entity<Review>(entity =>
        {
            entity.Property(e => e.Comment).HasMaxLength(1000);

            entity.HasOne(e => e.Reviewer)
                  .WithMany(e => e.ReviewsGiven)
                  .HasForeignKey(e => e.ReviewerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Reviewee)
                  .WithMany(e => e.ReviewsReceived)
                  .HasForeignKey(e => e.RevieweeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Booking)
                  .WithMany(e => e.Reviews)
                  .HasForeignKey(e => e.BookingId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.RevieweeId);
            entity.HasIndex(e => e.Rating);
            entity.HasIndex(e => e.ReviewType);
        });

        // Configure ChatMessage entity
        builder.Entity<ChatMessage>(entity =>
        {
            entity.Property(e => e.Content).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.AttachmentUrl).HasMaxLength(255);
            entity.Property(e => e.AttachmentType).HasMaxLength(100);

            entity.HasOne(e => e.Sender)
                  .WithMany(e => e.MessagesSent)
                  .HasForeignKey(e => e.SenderId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Receiver)
                  .WithMany(e => e.MessagesReceived)
                  .HasForeignKey(e => e.ReceiverId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Booking)
                  .WithMany(e => e.ChatMessages)
                  .HasForeignKey(e => e.BookingId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.BookingId);
            entity.HasIndex(e => e.SentAt);
            entity.HasIndex(e => new { e.SenderId, e.ReceiverId });
        });
    }
}
