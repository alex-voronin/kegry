using Microsoft.EntityFrameworkCore;
using SharedCore.Extensions;
using UserProfileModule.Domain.Aggregates;

namespace UserProfileModule.Infrastructure.Persistence;

public class UserProfileContext : DbContext
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Hobby> Hobbies => Set<Hobby>();

    public UserProfileContext(DbContextOptions<UserProfileContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("users");

        modelBuilder.Entity<UserProfile>(builder =>
        {
            builder.ToTable("profiles");
            builder.Property(up => up.Id).ValueGeneratedNever();
            builder.Property(up => up.FirstName)
                .IsRequired()
                .HasMaxLength(ValidationParameters.FirstNameMaxLength);
            builder.Property(up => up.LastName)
                .IsRequired()
                .HasMaxLength(ValidationParameters.LastNameMaxLength);
            builder.Property(up => up.Bio)
                .HasMaxLength(ValidationParameters.BioMaxLength);
            builder.Property(up => up.AvatarImageS3Key)
                .HasMaxLength(ValidationParameters.S3KeyMaxLength);
            builder.Property(up => up.CoverImageS3Key)
                .HasMaxLength(ValidationParameters.S3KeyMaxLength);

            builder.HasMany(up => up.Hobbies)
                .WithMany(h => h.UserProfiles)
                .UsingEntity(j => j.ToTable("user_profile_hobby_link"));
        });

        modelBuilder.Entity<Hobby>(builder =>
        {
            builder.ToTable("hobbies");
            builder.Property(h => h.Id).ValueGeneratedNever();
            builder.HasIndex(h => h.Name)
                .IsUnique();
        });

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