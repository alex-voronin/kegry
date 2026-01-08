using SecurityModule.Domain.Aggregates;
using SharedCore.Interfaces;

namespace SecurityModule.Domain.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    public Task<RefreshToken?> GetByTokenAsync(string token);
    public Task<RefreshToken?> GetByJtiAsync(string jti);
    public Task<int> TokensCountAsync(Guid userId);
    public Task DeleteByJtiAsync(string jti);
}

