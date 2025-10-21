using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartDonationSystem.Models;

namespace SmartDonationSystem.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Donation> Donations { get; set; }
    public DbSet<DonationRequest> DonationRequests { get; set; }
    public DbSet<NGO> NGOs { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(50);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.State).HasMaxLength(50);
            entity.Property(e => e.Country).HasMaxLength(50);

            // One-to-One relationship with NGO
            entity.HasOne(u => u.NGO)
                .WithOne(n => n.User)
                .HasForeignKey<NGO>(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure NGO entity
        builder.Entity<NGO>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Contact).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Website).HasMaxLength(200);
            entity.Property(e => e.RegistrationNumber).HasMaxLength(50);

            // One-to-Many relationship with Donations
            entity.HasMany(n => n.Donations)
                .WithOne(d => d.NGO)
                .HasForeignKey(d => d.NGOId)
                .OnDelete(DeleteBehavior.SetNull);

            // One-to-Many relationship with DonationRequests
            entity.HasMany(n => n.DonationRequests)
                .WithOne(dr => dr.NGO)
                .HasForeignKey(dr => dr.NGOId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Donation entity
        builder.Entity<Donation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.FoodType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.PickupAddress).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(200);

            // Many-to-One relationship with Donor (ApplicationUser)
            entity.HasOne(d => d.Donor)
                .WithMany(u => u.Donations)
                .HasForeignKey(d => d.DonorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Many-to-One relationship with NGO
            entity.HasOne(d => d.NGO)
                .WithMany(n => n.Donations)
                .HasForeignKey(d => d.NGOId)
                .OnDelete(DeleteBehavior.SetNull);

            // One-to-Many relationship with DonationRequests
            entity.HasMany(d => d.DonationRequests)
                .WithOne(dr => dr.Donation)
                .HasForeignKey(dr => dr.DonationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure DonationRequest entity
        builder.Entity<DonationRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ResponseMessage).HasMaxLength(200);

            // Many-to-One relationship with Donation
            entity.HasOne(dr => dr.Donation)
                .WithMany(d => d.DonationRequests)
                .HasForeignKey(dr => dr.DonationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Many-to-One relationship with NGO
            entity.HasOne(dr => dr.NGO)
                .WithMany(n => n.DonationRequests)
                .HasForeignKey(dr => dr.NGOId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Notification entity
        builder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.ActionUrl).HasMaxLength(200);
            entity.Property(e => e.RelatedEntityType).HasMaxLength(50);

            // Many-to-One relationship with ApplicationUser
            entity.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for performance
            entity.HasIndex(e => new { e.UserId, e.IsRead });
            entity.HasIndex(e => e.Timestamp);
        });
    }
}
