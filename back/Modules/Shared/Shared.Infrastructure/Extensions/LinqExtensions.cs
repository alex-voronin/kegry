using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Shared.Infrastructure.Extensions;

public static class LinqExtensions
{
    public static IQueryable<TEntity> ApplyNoTracking<TEntity>(
        this IQueryable<TEntity> query,
        bool applyNoTracking)
        where TEntity : class
    {
        return applyNoTracking ? query.AsNoTracking() : query;
    }

    public class ConditionalWhereQueryable<TSource>
    {
        public required IQueryable<TSource> Queryable { get; init; }
        public required Expression<Func<TSource, bool>> Predicate { get; init; }
        public IQueryable<TSource> ApplyIf(bool apply) => apply ? Queryable.Where(Predicate) : Queryable;

        internal ConditionalWhereQueryable()
        {
        }
    }

    public static ConditionalWhereQueryable<TSource> ConditionalWhere<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate)
        => new()
        {
            Queryable = source,
            Predicate = predicate,
        };
}