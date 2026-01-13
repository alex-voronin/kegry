using Microsoft.EntityFrameworkCore;
using SharedCore.Extensions;
using UserProfileModule.Domain.Aggregates;

namespace UserProfileModule.Infrastructure.Persistence;

public class UserProfileContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Hobby> Hobbies => Set<Hobby>();

    public UserProfileContext(DbContextOptions<UserProfileContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("user_profile");

        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users");
            builder.Property(u => u.Id).ValueGeneratedNever();
            builder.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(ValidationParameters.FirstNameMaxLength);
            builder.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(ValidationParameters.LastNameMaxLength);
            builder.Property(u => u.Bio)
                .HasMaxLength(ValidationParameters.BioMaxLength);
            builder.Property(u => u.AvatarImageS3Key)
                .HasMaxLength(ValidationParameters.S3KeyMaxLength);
            builder.Property(u => u.CoverImageS3Key)
                .HasMaxLength(ValidationParameters.S3KeyMaxLength);

            builder.HasMany(u => u.Hobbies)
                .WithMany(h => h.Users)
                .UsingEntity(j => j.ToTable("user_hobby_link"));

            builder.HasQueryFilter(u => !u.IsDeleted);
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