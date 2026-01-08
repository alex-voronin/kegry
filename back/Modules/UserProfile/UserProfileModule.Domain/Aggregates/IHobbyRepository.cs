using SharedCore.Interfaces;

namespace UserProfileModule.Domain.Aggregates;

public interface IHobbyRepository : IRepository<Hobby>
{
    public ValueTask<Hobby[]> GetAll(CancellationToken ct = default);
    public ValueTask<IReadOnlyCollection<Guid>> GetNonExistingIds(Guid[] hobbyIds, CancellationToken ct = default);
}