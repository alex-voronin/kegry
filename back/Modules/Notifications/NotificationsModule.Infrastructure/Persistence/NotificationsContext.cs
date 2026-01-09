using Microsoft.EntityFrameworkCore;
using NotificationModule.Domain.Aggregates;

namespace NotificationsModule.Infrastructure.Persistence;

public class NotificationsContext : DbContext
{
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationsSettings> NotificationsSettings => Set<NotificationsSettings>();

    public NotificationsContext(DbContextOptions<NotificationsContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notifications");

        modelBuilder.Entity<Device>(builder =>
        {
            builder.ToTable("devices");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.FcmToken).IsRequired().HasMaxLength(512);
            builder.Property(x => x.UserId).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();

            builder.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.Email).IsRequired().HasMaxLength(256);
            builder.Property(x => x.FirstName).HasMaxLength(100);
            builder.Property(x => x.LastName).HasMaxLength(100);
            builder.Property(x => x.AvatarImageUrl).HasMaxLength(1024);

            builder.HasMany(x => x.Devices)
                .WithOne()
                .HasForeignKey(x => x.UserId);

            builder.HasMany(x => x.Notifications)
                .WithOne()
                .HasForeignKey(x => x.UserId);

            builder.HasOne(x => x.NotificationsSettings)
                .WithOne()
                .HasForeignKey<NotificationsSettings>(x => x.UserId);
        });

        modelBuilder.Entity<NotificationsSettings>(builder =>
        {
            builder.ToTable("notification_settings");
            builder.HasKey(x => x.UserId);
            builder.Property(x => x.UserId).ValueGeneratedNever();
        });

        modelBuilder.Entity<Notification>(builder =>
        {
            builder.ToTable("notifications");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.UserId).IsRequired();
            builder.Property(x => x.PrimaryText).IsRequired().HasMaxLength(1024);
            builder.Property(x => x.SecondaryText).IsRequired().HasMaxLength(1024);
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.IsSeen).IsRequired();

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.FromUserId);

            builder.HasOne(x => x.FromUser)
                .WithMany()
                .HasForeignKey(x => x.FromUserId);
        });

        base.OnModelCreating(modelBuilder);
    }
}
