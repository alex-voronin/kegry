using Microsoft.EntityFrameworkCore;
using NotificationModule.Domain.Aggregates;

namespace NotificationsModule.Infrastructure.Persistence;

public class NotificationsContext : DbContext
{
    public DbSet<Device> Devices => Set<Device>();

    public NotificationsContext(DbContextOptions<NotificationsContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notifications");

        modelBuilder.Entity<Device>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.FcmToken).IsRequired().HasMaxLength(512);
            builder.Property(x => x.UserId).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            
            builder.HasIndex(x => x.UserId);
        });

        base.OnModelCreating(modelBuilder);
    }
}
