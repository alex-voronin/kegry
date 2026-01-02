using FeedModule.Domain.Aggregates;
using FeedModule.Domain.Aggregates.ReadOnly;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;

namespace FeedModule.Infrastructure.Persistence;

public class FeedContext: DbContext
{
    // rw
    public DbSet<FeedProfile> FeedProfiles => Set<FeedProfile>();
    public DbSet<Filter> Filters => Set<Filter>();
    public DbSet<DisplaySettings> DisplaySettings => Set<DisplaySettings>();
    
    //ro
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Hobby> Hobbies => Set<Hobby>();
    public DbSet<Location> Locations => Set<Location>();

    public FeedContext(DbContextOptions<FeedContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDbFunction(typeof(PostgisFunctions).GetMethod(nameof(PostgisFunctions.DistanceSphere))!)
            .HasName("ST_DistanceSphere")
            .HasSchema("public");
        
        modelBuilder.Entity<FeedProfile>(builder =>
        {
            builder.ToTable("profiles", "feed");
            builder.Property(fp => fp.Id)
                .ValueGeneratedNever();
            builder.Property(fp => fp.Rating)
                .HasColumnType("numeric(2,1)");
        });

        modelBuilder.Entity<Filter>(builder =>
        {
            builder.ToTable("filters", "feed");
            builder.HasKey(f => f.UserId);
            
            builder.Property(f => f.UserId)
                .ValueGeneratedNever();
            
            builder.Property(f => f.HobbyIds)
                .HasColumnType("uuid[]");
        });

        modelBuilder.Entity<DisplaySettings>(builder =>
        {
            builder.ToTable("display_settings", "feed");
            builder.HasKey(ds => ds.UserId);
            
            builder.Property(ds => ds.UserId)
                .ValueGeneratedNever();
        });
        
        modelBuilder.Entity<UserProfile>(builder =>
        {
            builder.ToTable("user_profiles", "users");
            builder.HasKey(e => e.Id);

            builder.HasMany(up => up.Hobbies)
                .WithMany(h => h.UserProfiles)
                .UsingEntity(j =>
                {
                    j.ToTable("user_profile_hobby_link");
                    j.Metadata.SetIsTableExcludedFromMigrations(true);
                });
            
            builder.Metadata.SetIsTableExcludedFromMigrations(true);
        });

        modelBuilder.Entity<Hobby>(builder =>
        {
            builder.ToTable("hobbies", "users");
            builder.HasKey(e => e.Id);

            builder.Metadata.SetIsTableExcludedFromMigrations(true);
        });

        modelBuilder.Entity<Location>(builder =>
        {
            builder.ToTable("locations", "location");
            builder.HasKey(e => e.UserId);

            builder.Property(l => l.Coordinates)
                .HasColumnType("geometry(Point, 4326)")
                .IsRequired();

            builder.Metadata.SetIsTableExcludedFromMigrations(true);
        });
    }
}