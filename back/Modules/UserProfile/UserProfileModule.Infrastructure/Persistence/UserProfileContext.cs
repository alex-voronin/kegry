using Microsoft.EntityFrameworkCore;
using SharedCore.Extensions;
using UserProfileModule.Domain.Aggregates;

namespace UserProfileModule.Infrastructure.Persistence;

public class UserProfileContext : DbContext
{
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Hobby> Hobbies { get; set; }
    
    public UserProfileContext(DbContextOptions<UserProfileContext> options)
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
        
        modelBuilder.Entity<UserProfile>()
            .Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(ValidationParameters.FirstNameMaxLength);
        
        modelBuilder.Entity<UserProfile>()
            .Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(ValidationParameters.LastNameMaxLength);
        
        modelBuilder.Entity<UserProfile>()
            .Property(x => x.Bio)
            .IsRequired()
            .HasMaxLength(ValidationParameters.BioMaxLength);

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
}