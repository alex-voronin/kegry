using SharedCore.Interfaces;
using UserProfileModule.Domain.Aggregates;

namespace UserProfileModule.Domain.Repostories;

public interface IHobbyRepository : IRepository<Hobby>
{
    public ValueTask<Hobby[]> GetAll(CancellationToken ct = default);
    public ValueTask<IReadOnlyCollection<Guid>> GetNonExistingIds(Guid[] hobbyIds, CancellationToken ct = default);
}