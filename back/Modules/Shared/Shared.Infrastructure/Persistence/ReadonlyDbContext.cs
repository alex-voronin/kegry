using Microsoft.EntityFrameworkCore;

namespace Shared.Infrastructure.Persistence;

public class ReadonlyDbContext : DbContext
{
    public ReadonlyDbContext(DbContextOptions options) : base(options)
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        // ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public override int SaveChanges()
    {
        throw new InvalidOperationException("You cannot save a read only context");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("You cannot save a read only context");
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("You cannot save a read only context");
    }
}