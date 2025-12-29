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
        
        modelBuilder.Entity<Location>(builder =>
        {
            builder.ToTable("locations");
            builder.HasKey(l => l.UserId);
        });
    }
}
