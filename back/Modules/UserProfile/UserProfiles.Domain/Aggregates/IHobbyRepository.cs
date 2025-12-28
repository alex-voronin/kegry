using SharedCore.Interfaces;

namespace UserProfiles.Domain.Aggregates;

public interface IHobbyRepository : IRepository<Hobby>
{
    Task<Hobby[]> GetAll(CancellationToken ct = default);
}