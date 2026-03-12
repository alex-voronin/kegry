using Microsoft.EntityFrameworkCore;
using LocationModule.Domain.Aggregates;

namespace LocationModule.Infrastructure.Persistence;

public class LocationContext : DbContext
{
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<TrackedUserPair> TrackedUsers => Set<TrackedUserPair>();

    public LocationContext(DbContextOptions<LocationContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("location");
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<Location>(builder =>
        {
            builder.ToTable("locations");
            builder.HasKey(l => l.UserId);

            builder.Property(l => l.Coordinates)
                .HasColumnType("geography(Point, 4326)")
                .IsRequired();

            builder.HasIndex(l => l.Coordinates)
                .HasMethod("gist");

            builder.HasIndex(l => l.UserId)
                .IncludeProperties(l => l.Coordinates)
                .HasDatabaseName("ix_locations_userid_covering");
        });

        modelBuilder.Entity<TrackedUserPair>(builder =>
        {
            builder.ToTable("tracked_users");
            builder.HasKey(tp => new { tp.FirstUserId, tp.SecondUserId });
            builder.HasIndex(tp => tp.FirstUserId);
            builder.HasIndex(tp => tp.SecondUserId);
            builder.HasIndex(tp => tp.ExpiresAt);
        });
    }
}
