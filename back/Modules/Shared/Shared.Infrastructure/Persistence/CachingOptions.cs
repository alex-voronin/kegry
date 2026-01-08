using Microsoft.Extensions.Caching.Hybrid;

namespace Shared.Infrastructure.Persistence;

public static class CachingOptions
{
    public static readonly HybridCacheEntryOptions Week = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromDays(7),
        LocalCacheExpiration = TimeSpan.FromDays(7)
    };
}