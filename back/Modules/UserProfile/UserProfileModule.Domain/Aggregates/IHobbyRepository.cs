using SharedCore.Interfaces;

namespace UserProfileModule.Domain.Aggregates;

public interface IHobbyRepository : IRepository<Hobby>
{
    Task<Hobby[]> GetAll(CancellationToken ct = default);
}