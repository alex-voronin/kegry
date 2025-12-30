using Microsoft.Extensions.Caching.Hybrid;

namespace Shared.Infrastructure.Persistence;

public static class CachingOptions
{
    public static readonly HybridCacheEntryOptions Forever = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromDays(365 * 100),
        LocalCacheExpiration = TimeSpan.FromDays(365 * 100)
    };
}