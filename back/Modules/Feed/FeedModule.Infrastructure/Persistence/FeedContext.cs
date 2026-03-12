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
    public DbSet<User> Users => Set<User>();
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
        Type[] readOnlyTypes = [typeof(User), typeof(Hobby), typeof(Location)];

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
        modelBuilder.Entity<FeedUserRelation>(builder =>
        {
            builder.ToTable("user_relations", "feed");
            builder.Property(r => r.Id)
                .ValueGeneratedNever();
            builder.Property(r => r.Rating)
                .HasColumnType("numeric(2,1)");

            builder.HasIndex(r => r.MyUserId)
                .HasFilter("\"is_in_meeting\" = true")
                .IsUnique()
                .HasDatabaseName("unq_feed_user_relation_state_active_meeting_per_user");

            builder.HasIndex(r => new { r.MyUserId, r.OtherUserId })
                .IsUnique()
                .HasDatabaseName("unq_feed_user_relation_pair");
            // Indexes to speedup EfCoreFeedUserRelationRepository.GetMadeInvitesCountAsync queries // 
            builder.HasIndex(r => new { r.MyUserId, r.InvitedAt })
                .HasDatabaseName("IX_FeedUserRelations_MyUserId_InvitedAt")
                .IncludeProperties(r => new { r.OtherUserId, r.IsBlocked });

            builder.HasIndex(r => new { r.MyUserId, r.IsInMeeting })
                .HasDatabaseName("IX_FeedUserRelations_MyUserId_IsInMeeting");
            ////////////////////////////////////////////////////////////////////////////////

            builder.Property(r => r.ReportMessage)
                .HasMaxLength(ValidationParameters.MaxReportMessageLength);

            builder.HasIndex(r => r.InvitedAt);
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

        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users", "user_profile");
            builder.HasKey(u => u.Id);

            builder.HasMany(u => u.Hobbies)
                .WithMany(h => h.Users)
                .UsingEntity(j =>
                {
                    j.ToTable("user_hobby_link", "user_profile");
                    j.Metadata.SetIsTableExcludedFromMigrations(true);
                });

            builder.HasQueryFilter(u => !u.IsDeleted);
            builder.Metadata.SetIsTableExcludedFromMigrations(true);
        });

        modelBuilder.Entity<Hobby>(builder =>
        {
            builder.ToTable("hobbies", "user_profile");
            builder.HasKey(e => e.Id);

            builder.Metadata.SetIsTableExcludedFromMigrations(true);
        });

        modelBuilder.Entity<Location>(builder =>
        {
            builder.ToTable("locations", "location");
            builder.HasKey(e => e.UserId);

            builder.Property(l => l.Coordinates)
                .HasColumnType("geography(Point, 4326)")
                .IsRequired();

            builder.Metadata.SetIsTableExcludedFromMigrations(true);
        });
    }
}