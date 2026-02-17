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
                    j =>
                    {
                        j.HasKey("user_id", "npc_id");
                    });

            builder.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<Npc>(builder =>
        {
            builder.ToTable("npcs");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.NameKey).IsRequired().HasMaxLength(100);
            builder.Property(x => x.AvatarUrl).IsRequired().HasMaxLength(500);
            builder.Property(x => x.MonologKey).IsRequired();

            const string gobbySnugsKey = "GobbySnugs.Name";
            builder.HasData(new Npc
            {
                Id = gobbySnugsKey.ToDeterministicGuid(),
                NameKey = gobbySnugsKey,
                AvatarUrl = "assets/npc/gobby-snugs.jpg",
                MonologKey = "GobbySnugs.Monolog"
            });
        });
    }
}