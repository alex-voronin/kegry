using Microsoft.EntityFrameworkCore;

namespace Shared.Infrastructure.Extensions;

public static class DbContextExtensions
{
    public static bool IsEntityTracked<T>(this DbContext context, T entity)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context), "Parameter cannot be null.");
        }

        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity), "Parameter cannot be null.");
        }

        var entry = context.Entry(entity);
        return entry.State != EntityState.Detached;
    }
}