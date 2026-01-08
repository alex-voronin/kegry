using SecurityModule.Domain.Aggregates;
using SharedCore.Interfaces;

namespace SecurityModule.Domain.Repositories;

public interface IUserRepository : IRepository<User>
{
    public Task<bool> IsUserExistAsync(string email, CancellationToken ct = default);
}