using Microsoft.EntityFrameworkCore;
using SecurityModule.Domain.Aggregates;
using SecurityModule.Domain.Repositories;
using Shared.Infrastructure.Persistence;

namespace SecurityModule.Infrastructure.Persistence.Repositories;

internal class EfCoreRefreshTokenRepository : Repository<SecurityContext, RefreshToken>, IRefreshTokenRepository
{
    public EfCoreRefreshTokenRepository(SecurityContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await Context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Token == token);
    }

    public async Task<RefreshToken?> GetByJtiAsync(string jti)
    {
        return await Context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Jti == jti);
    }

    public async Task<int> TokensCountAsync(Guid userId)
    {
        return await Context.RefreshTokens
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .CountAsync();
    }

    public async Task DeleteByJtiAsync(string jti)
    {
        await Context.RefreshTokens.Where(t => t.Jti == jti).ExecuteDeleteAsync();
    }
}