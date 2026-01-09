using Microsoft.EntityFrameworkCore;
using LocationModule.Domain.Aggregates;

namespace LocationModule.Infrastructure.Persistence;

public class LocationContext : DbContext
{
    public DbSet<Location> Locations => Set<Location>();

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
                .HasColumnType("geometry(Point, 4326)")
                .IsRequired();

            builder.HasIndex(l => l.Coordinates)
                .HasMethod("gist");
        });
    }
}
