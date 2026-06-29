using EventModule.Domain.Aggregates;
using EventModule.Domain.Aggregates.ReadOnly;
using Microsoft.EntityFrameworkCore;

namespace EventModule.Infrastructure.Persistence;

public class EventContext : DbContext
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventContact> EventContacts => Set<EventContact>();
    public DbSet<EventHobbyLink> EventHobbyLinks => Set<EventHobbyLink>();
    public DbSet<EventParticipant> EventParticipants => Set<EventParticipant>();

    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<FeedUserRelation> FeedUserRelations => Set<FeedUserRelation>();

    public EventContext(DbContextOptions<EventContext> options) : base(options)
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
        Type[] readOnlyTypes = [typeof(Venue), typeof(Company), typeof(FeedUserRelation)];

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
        modelBuilder.Entity<Event>(builder =>
        {
            builder.ToTable("events", "event");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .ValueGeneratedNever();

            builder.Property(e => e.CreatorAvatarS3Key)
                .HasMaxLength(500);

            builder.Property(e => e.CoverImageS3Key)
                .HasMaxLength(500);

            builder.Property(e => e.Title)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(e => e.Description)
                .HasMaxLength(2000)
                .IsRequired();

            builder.Property(e => e.StartDateTime)
                .IsRequired();

            builder.Property(e => e.Type)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.StartDateTime)
                .IsRequired();

            builder.Property(e => e.UpdatedAt)
                .IsConcurrencyToken();

            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false);

            builder.HasQueryFilter(e => !e.IsDeleted);

            builder.Property(e => e.IsVisibleOnMap)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.RequiresApproval)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.MaxParticipants);

            builder.HasMany(e => e.EventContacts)
                .WithOne()
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(e => e.EventHobbyLinks)
                .WithOne()
                .HasForeignKey(h => h.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(e => e.EventParticipants)
                .WithOne()
                .HasForeignKey(p => p.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(e => new { e.VenueId, e.StartDateTime })
                .HasDatabaseName("ix_events_venue_id_start_date");

            builder.HasIndex(e => new { e.CreatorId, e.StartDateTime })
                .HasDatabaseName("ix_events_creator_id_start_date");

            builder.HasIndex(e => e.StartDateTime)
                .HasDatabaseName("ix_events_start_date");
        });

        modelBuilder.Entity<EventContact>(builder =>
        {
            builder.ToTable("event_contacts", "event");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id)
                .ValueGeneratedNever();

            builder.Property(c => c.Type)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(c => c.Value)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(c => c.IsPublic)
                .IsRequired()
                .HasDefaultValue(false);
        });

        modelBuilder.Entity<EventHobbyLink>(builder =>
        {
            builder.ToTable("event_hobby_links", "event");
            builder.HasKey(h => new { h.EventId, h.HobbyId });

            builder.HasIndex(h => new { h.HobbyId, h.EventId })
                .HasDatabaseName("ix_event_hobby_links_hobby_id_event_id");
        });

        modelBuilder.Entity<EventParticipant>(builder =>
        {
            builder.ToTable("event_participants", "event");
            builder.HasKey(p => new { p.EventId, p.UserId });

            builder.Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(p => p.JoinedAt)
                .IsRequired();

            builder.HasIndex(p => new { p.UserId, p.Status })
                .HasDatabaseName("ix_event_participants_user_id_status");

            builder.HasIndex(p => new { p.UserId, p.EventId })
                .HasDatabaseName("ix_event_participants_user_id_event_id");
        });

        modelBuilder.Entity<Venue>(builder =>
        {
            builder.ToTable("venues", "venue");
            builder.HasKey(v => v.Id);

            builder.Property(v => v.Coordinates)
                .HasColumnType("geography(Point, 4326)")
                .IsRequired();

            builder.HasQueryFilter(v => !v.IsDeleted);
            builder.Metadata.SetIsTableExcludedFromMigrations(true);
        });

        modelBuilder.Entity<Company>(builder =>
        {
            builder.ToTable("companies", "venue");
            builder.HasKey(c => c.Id);

            builder.HasQueryFilter(c => !c.IsDeleted);
            builder.Metadata.SetIsTableExcludedFromMigrations(true);
        });

        modelBuilder.Entity<FeedUserRelation>(builder =>
        {
            builder.ToTable("user_relations", "feed");
            builder.HasKey(r => r.Id);
            builder.Metadata.SetIsTableExcludedFromMigrations(true);
        });
    }
}
