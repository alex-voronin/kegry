using EngagementModule.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using SharedCore.Extensions;

namespace EngagementModule.Infrastructure.Persistence;

public class EngagementContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Npc> Npcs => Set<Npc>();

    public EngagementContext(DbContextOptions<EngagementContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("engagement");

        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            modelBuilder.Entity<User>()
                .HasMany(u => u.MetNpcs)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "user_met_npcs",
                    j => j
                        .HasOne<Npc>()
                        .WithMany()
                        .HasForeignKey("npc_id")
                        .HasPrincipalKey(n => n.Id),
                    j => j
                        .HasOne<User>()
                        .WithMany()
                        .HasForeignKey("user_id")
                        .HasPrincipalKey(u => u.Id),
                    j => { j.HasKey("user_id", "npc_id"); });

            builder.HasIndex(x => x.IsDeleted);
            builder.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<Npc>(builder =>
        {
            builder.ToTable("npcs");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.NameKey).IsRequired().HasMaxLength(100);
            builder.Property(x => x.AvatarUrl).IsRequired().HasMaxLength(512);
            builder.Property(x => x.CoverUrl).IsRequired().HasMaxLength(512);
            builder.Property(x => x.MonologKey).IsRequired();

            const string cornKey = "Corn.Name";
            const string cockatriceKey = "Cockatrice.Name";
            builder.HasData(
                new Npc
                {
                    Id = cornKey.ToDeterministicGuid(),
                    IsSecret = false,
                    NameKey = cornKey,
                    AvatarUrl = "/assets/npc/corn-tavern/avatar.webp",
                    CoverUrl = "/assets/npc/corn-tavern/cover.webp",
                    MonologKey = "Corn.Monolog"
                },
                new Npc
                {
                    Id = cockatriceKey.ToDeterministicGuid(),
                    IsSecret = true,
                    NameKey = cockatriceKey,
                    AvatarUrl = "/assets/npc/cockatrice/avatar.webp",
                    CoverUrl = "/assets/npc/cockatrice/cover.webp",
                    // just in case if we need this monolog later. For now a user can't see it, so the key isn't added 
                    // to localization resources
                    MonologKey = "Cockatrice.Monolog"
                }
            );
        });
    }
}