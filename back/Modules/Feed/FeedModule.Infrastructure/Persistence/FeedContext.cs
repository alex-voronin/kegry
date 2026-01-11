using FeedModule.Domain.Aggregates;
using FeedModule.Domain.Aggregates.ReadOnly;
using Microsoft.EntityFrameworkCore;

namespace FeedModule.Infrastructure.Persistence;

public class FeedContext : DbContext
{
    // rw
    public DbSet<FeedUserRelation> FeedUserRelations => Set<FeedUserRelation>();
    public DbSet<Filter> Filters => Set<Filter>();
    public DbSet<DisplaySettings> DisplaySettings => Set<DisplaySettings>();
    public DbSet<UserRating> UserRatings => Set<UserRating>();

    //ro
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Hobby> Hobbies => Set<Hobby>();
    public DbSet<Location> Locations => Set<Location>();

    public FeedContext(DbContextOptions<FeedContext> options) : base(options)
    {
    }

    public override int SaveChanges()
    {
        ThrowOnReadOnlyEntityModification();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ThrowOnReadOnlyEntityModification();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ThrowOnReadOnlyEntityModification()
    {
        Type[] readOnlyTypes = [typeof(UserProfile), typeof(Hobby), typeof(Location)];

        var forbiddenChanges = ChangeTracker.Entries()
            .Where(e => readOnlyTypes.Contains(e.Entity.GetType()) &&
                        (e.State == EntityState.Added ||
                         e.State == EntityState.Modified ||
                         e.State == EntityState.Deleted));

        if (forbiddenChanges.Any())
        {
            throw new InvalidOperationException("Attempted to modify a read-only entity.");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDbFunction(typeof(PostgisFunctions).GetMethod(nameof(PostgisFunctions.DistanceSphere))!)
            .HasName("ST_DistanceSphere")
            .HasSchema("public");

        modelBuilder.Entity<FeedUserRelation>(builder =>
        {
            builder.ToTable("user_relations", "feed");
            builder.Property(fp => fp.Id)
                .ValueGeneratedNever();
            builder.Property(fp => fp.Rating)
                .HasColumnType("numeric(2,1)");

            builder.HasIndex(fp => fp.MyUserId)
                .HasFilter("\"is_in_meeting\" = true")
                .IsUnique()
                .HasDatabaseName("unq_feed_user_relation_state_active_meeting_per_user");

            builder.HasIndex(fp => new { fp.MyUserId, fp.OtherUserId })
                .IsUnique()
                .HasDatabaseName("unq_feed_user_relation_pair");
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

        modelBuilder.Entity<UserRating>(builder =>
        {
            builder.ToTable("user_ratings", "feed");
            builder.HasKey(ds => ds.UserId);

            builder.Property(ds => ds.UserId)
                .ValueGeneratedNever();
        });

        // Read-only entities configuration

        modelBuilder.Entity<UserProfile>(builder =>
        {
            builder.ToTable("profiles", "users");
            builder.HasKey(up => up.Id);

            builder.HasMany(up => up.Hobbies)
                .WithMany(h => h.UserProfiles)
                .UsingEntity(j =>
                {
                    j.ToTable("user_profile_hobby_link", "users");
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