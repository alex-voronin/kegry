using Microsoft.EntityFrameworkCore;
using VenueModule.Domain.Aggregates;

namespace VenueModule.Infrastructure.Persistence;

public class VenueContext : DbContext
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<LoyaltyLevel> LoyaltyLevels => Set<LoyaltyLevel>();
    public DbSet<Promo> Promos => Set<Promo>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<DiscountDay> DiscountDays => Set<DiscountDay>();
    public DbSet<UserLoyalty> UserLoyalties => Set<UserLoyalty>();

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
        ConfigureDiscountDay(modelBuilder);
        ConfigureUserLoyalty(modelBuilder);
        ConfigurePromo(modelBuilder);
    }

    private static void ConfigureCompany(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("companies");
            entity.HasKey(c => c.Id);
            entity.Property(h => h.Id).ValueGeneratedNever();
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
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(c => c.OwnerId);
            entity.HasMany(v => v.LoyaltyLevels)
                .WithOne()
                .HasForeignKey(l => l.CompanyId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(u => u.IsDeleted);
            entity.HasQueryFilter(c => !c.IsDeleted);
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
            entity.HasMany(v => v.Discounts)
                .WithOne()
                .HasForeignKey(d => d.VenueId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(v => v.Promos)
                .WithOne()
                .HasForeignKey(d => d.VenueId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(v => v.CompanyId);
            entity.HasIndex(v => v.Coordinates)
                .HasMethod("gist");
            entity.HasIndex(u => u.QrCode);

            entity.HasIndex(u => u.IsVerified);
            entity.HasQueryFilter(NamedFilters.Venue.Verified, u => u.IsVerified);

            entity.HasIndex(u => u.IsDeleted);
            entity.HasQueryFilter(NamedFilters.Venue.SoftDeleted, u => !u.IsDeleted);
        });
    }

    private static void ConfigureVisit(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Visit>(entity =>
        {
            entity.ToTable("visits");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.UserId)
                .IsRequired();
            entity.Property(v => v.VenueId)
                .IsRequired();
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
            entity.HasKey(l => new { l.Id, l.CompanyId });
            entity.ToTable(t => t.HasCheckConstraint("CK_loyalty_levels_id", "\"id\" IN (0, 1, 2, 3)"));

            entity.HasQueryFilter(c => !c.IsActive);
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
                .HasForeignKey(d => new { d.LoyaltyLevelId, d.CompanyId })
                .IsRequired()
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(d => d.Venue)
                .WithMany(v => v.Discounts)
                .HasForeignKey(d => d.VenueId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(d => d.Days)
                .WithOne()
                .HasForeignKey(dd => dd.DiscountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureDiscountDay(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DiscountDay>(entity =>
        {
            entity.ToTable("discount_days");
            entity.HasKey(d => new { d.DiscountId, d.DayOfWeek });

            // Enforce that DayOfWeek is exactly 0-6 (Sunday-Saturday)
            entity.ToTable(t =>
                t.HasCheckConstraint("CK_discount_days_day_of_week", "\"day_of_week\" IN (0, 1, 2, 3, 4, 5, 6)"));
        });
    }

    private static void ConfigureUserLoyalty(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserLoyalty>(entity =>
        {
            entity.ToTable("user_loyalties");
            entity.HasKey(u => new { u.UserId, u.CompanyId });
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<LoyaltyLevel>()
                .WithMany()
                .HasForeignKey(u => new { u.LoyaltyLevelId, u.CompanyId })
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigurePromo(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Promo>(entity =>
        {
            entity.ToTable("promo");
            entity.HasKey(p => p.Id);
            entity.HasOne(p => p.Venue)
                .WithMany(v => v.Promos)
                .HasForeignKey(d => d.VenueId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}