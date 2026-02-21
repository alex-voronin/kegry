using Microsoft.EntityFrameworkCore;
using SharedCore.Extensions;
using UserProfileModule.Domain.Aggregates;

namespace UserProfileModule.Infrastructure.Persistence;

public class UserProfileContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Hobby> Hobbies => Set<Hobby>();
    public DbSet<HobbyCategory> HobbyCategories => Set<HobbyCategory>();

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

        modelBuilder.Entity<HobbyCategory>(builder =>
        {
            builder.ToTable("hobby_categories");
            builder.Property(c => c.Id).ValueGeneratedNever();
            builder.Property(c => c.Name).HasMaxLength(100);
            builder.HasIndex(c => c.Name)
                .IsUnique();
        });

        modelBuilder.Entity<Hobby>(builder =>
        {
            builder.ToTable("hobbies");
            builder.Property(h => h.Id).ValueGeneratedNever();
            builder.Property(h => h.Name).HasMaxLength(100);
            builder.HasIndex(h => h.Name)
                .IsUnique();

            builder.HasOne(h => h.Category)
                .WithMany(c => c.Hobbies)
                .HasForeignKey(h => h.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var categories = Enum.GetValues<SharedCore.ValueObjects.HobbyCategory>();
        var categoryEntities = categories.Select(c =>
            {
                var name = Enum.GetName(c)!.ToLower();
                return new HobbyCategory()
                {
                    Id = name.ToDeterministicGuid(),
                    Name = name
                };
            })
            .ToArray();
        modelBuilder.Entity<HobbyCategory>().HasData(categoryEntities);

        var hobbies = Enum.GetValues<SharedCore.ValueObjects.Hobby>();
        var entities = hobbies.Select(h =>
            {
                var name = Enum.GetName(h)!.ToLower();
                var categoryName = Enum.GetName(SharedCore.ValueObjects.HobbyMetadata.GetCategory(h))!.ToLower();
                return new Hobby()
                {
                    Id = name.ToDeterministicGuid(),
                    Name = name,
                    CategoryId = categoryName.ToDeterministicGuid()
                };
            })
            .ToArray();
        modelBuilder.Entity<Hobby>().HasData(entities);
    }
}