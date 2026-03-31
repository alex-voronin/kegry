using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SharedCore.Interfaces;

namespace Shared.Infrastructure.Persistence.Audit;

public class AuditableInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;

        foreach (var entry in eventData.Context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                if (entry.Entity.UpdatedAt == default)
                {
                    entry.Entity.UpdatedAt = now;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                if (!entry.Property(e => e.UpdatedAt).IsModified)
                {
                    entry.Entity.UpdatedAt = now;
                }

                entry.Property(x => x.CreatedAt).IsModified = false; // just to ignore updates of CreatedAt
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}