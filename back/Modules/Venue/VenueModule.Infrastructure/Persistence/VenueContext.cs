using Microsoft.EntityFrameworkCore;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.ValueObjects;

namespace VenueModule.Infrastructure.Persistence;

public class VenueContext : DbContext
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Visit> Visits => Set<Visit>();

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
            entity.Property(c => c.ChangedAt)
                .IsRequired()
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasMany(c => c.Venues)
                .WithOne(v => v.Company)
                .HasForeignKey(v => v.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
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
            entity.Property(v => v.GoogleMapsUrl)
                .HasMaxLength(500);
            entity.Property(v => v.InstagramUrl)
                .HasMaxLength(200);
            entity.Property(v => v.PhoneNumber)
                .HasMaxLength(20);
            entity.Property(v => v.CompanyId)
                .IsRequired();
            entity.Property(v => v.Coordinates)
                .IsRequired();
            entity.HasIndex(v => v.CompanyId);
            entity.Property(c => c.ChangedAt)
                .IsRequired()
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.OwnsMany(v => v.Promos, promo =>
            {
                promo.ToTable("venue_promos");
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
}