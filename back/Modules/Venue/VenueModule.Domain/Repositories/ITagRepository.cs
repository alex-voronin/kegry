using SharedCore.Interfaces;
using VenueModule.Domain.Aggregates;

namespace VenueModule.Domain.Repositories;

public interface ITagRepository : IRepository<Tag>
{
    ValueTask<Tag[]> GetAllAsync(CancellationToken ct);
}
