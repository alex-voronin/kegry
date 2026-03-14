using Microsoft.EntityFrameworkCore;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.ValueObjects;

namespace VenueModule.Infrastructure.Persistence;

public class VenueContext : DbContext
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<LoyaltyLevel> LoyaltyLevels => Set<LoyaltyLevel>();
    public DbSet<Discount> Discounts => Set<Discount>();

    public VenueContext(DbContextOptions<VenueContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("venue");
        ConfigureCompany(modelBuilder);
        ConfigureVenue(modelBuilder);
        ConfigureVisit(modelBuilder);
        ConfigureLoyaltyLevel(modelBuilder);
        ConfigureDiscount(modelBuilder);
    }

    private static void ConfigureCompany(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("companies");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(c => c.OwnerId)
                .IsRequired();
            entity.Property(c => c.Email)
                .HasMaxLength(200);
            entity.Property(c => c.AvatarImageS3Key)
                .HasMaxLength(500);
            entity.HasMany(c => c.Venues)
                .WithOne(v => v.Company)
                .HasForeignKey(v => v.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => c.Name).IsUnique();
        });
    }

    private static void ConfigureVenue(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Venue>(entity =>
        {
            entity.ToTable("venues");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.CoverImageS3Key)
                .HasMaxLength(500);
            entity.Property(v => v.Address)
                .IsRequired()
                .HasMaxLength(500);
            entity.Property(v => v.WebsiteUrl)
                .HasMaxLength(500);
            entity.Property(v => v.GoogleMapsUrl)
                .HasMaxLength(500);
            entity.Property(v => v.InstagramUrl)
                .HasMaxLength(200);
            entity.Property(v => v.PhoneNumber)
                .HasMaxLength(20);
            entity.Property(v => v.WorkingHours)
                .HasMaxLength(500);
            entity.Property(v => v.CompanyId)
                .IsRequired();
            entity.Property(v => v.Coordinates)
                .HasColumnType("geography(Point, 4326)")
                .IsRequired();
            entity.Property(v => v.TimeZoneId)
                .IsRequired()
                .HasMaxLength(100);
            entity.HasIndex(v => v.CompanyId);
            entity.HasIndex(v => v.Coordinates)
                .HasMethod("gist");
            entity.HasMany(v => v.Discounts)
                .WithOne()
                .HasForeignKey(d => d.VenueId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.OwnsMany(v => v.Promos, promo =>
            {
                promo.ToTable("venue_promos");
                promo.Property(p => p.Body)
                    .HasMaxLength(1000);
            });
        });
    }

    private static void ConfigureVisit(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Visit>(entity =>
        {
            entity.ToTable("visits");
            entity.HasKey(v => new { v.UserId, v.VenueId });
            entity.Property(v => v.CheckedInAt)
                .IsRequired();
            entity.HasOne<Venue>()
                .WithMany()
                .HasForeignKey(v => v.VenueId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureLoyaltyLevel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LoyaltyLevel>(entity =>
        {
            entity.ToTable("loyalty_levels");
            entity.HasKey(l => new { l.Id, l.VenueId });
            entity.ToTable(t => t.HasCheckConstraint("CK_loyalty_levels_Id", "\"Id\" IN (0, 1, 2)"));
            entity.HasOne<Venue>()
                .WithMany()
                .HasForeignKey(l => l.VenueId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureDiscount(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Discount>(entity =>
        {
            entity.ToTable("discounts");
            entity.HasKey(d => d.Id);
            entity.HasOne(d => d.LoyaltyLevel)
                .WithMany()
                .HasForeignKey(d => new { d.LoyaltyLevelId, d.VenueId })
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Venue>()
                .WithMany()
                .HasForeignKey(d => d.VenueId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}