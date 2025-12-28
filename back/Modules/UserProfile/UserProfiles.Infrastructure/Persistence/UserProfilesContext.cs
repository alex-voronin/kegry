using Microsoft.EntityFrameworkCore;
using SharedCore.Extensions;
using UserProfiles.Domain.Aggregates;

namespace UserProfiles.Infrastructure.Persistence;

public class UserProfilesContext : DbContext
{
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Hobby> Hobbies { get; set; }
    
    public UserProfilesContext(DbContextOptions<UserProfilesContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("users");
        
        modelBuilder.Entity<UserProfile>()
            .ToTable("profiles");
        
        modelBuilder.Entity<Hobby>()
            .ToTable("hobbies");

        modelBuilder.Entity<Hobby>()
            .HasIndex(h => h.Name)
            .IsUnique();
        
        modelBuilder.Entity<UserProfile>()
            .HasMany(u => u.Hobbies)
            .WithMany(h => h.UserProfiles)
            .UsingEntity(j => j.ToTable("user_profile_hobby_link"));

        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var hobbies = Enum.GetValues<SharedCore.ValueObjects.Hobby>();
        var entities = hobbies.Select(h =>
            {
                var name = Enum.GetName(h)!.ToLower();
                return new Hobby()
                {
                    Id = name.ToDeterministicGuid(),
                    Name = name
                };
            })
            .ToArray();
        modelBuilder.Entity<Hobby>().HasData(entities);
    }
    
    private static Guid CreateGuidFromId(int id)
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(id).CopyTo(bytes, 0);
        return new Guid(bytes);
    }
}