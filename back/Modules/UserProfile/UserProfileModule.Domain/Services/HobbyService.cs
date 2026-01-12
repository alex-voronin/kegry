using SharedCore.Services.LifeTimes;
using UserProfileModule.Domain.Aggregates;
using UserProfileModule.Domain.Exceptions;
using UserProfileModule.Domain.Repostories;

namespace UserProfileModule.Domain.Services;

public interface IHobbyService
{
    public Task<Hobby[]> BuildHobbyStubs(Guid[] hobbyIds, CancellationToken ct = default);
}

internal class HobbyService : IHobbyService, IScoped
{
    private readonly IHobbyRepository _hobbyRepository;

    public HobbyService(IHobbyRepository hobbyRepository)
    {
        this._hobbyRepository = hobbyRepository;
    }

    public async Task<Hobby[]> BuildHobbyStubs(Guid[] hobbyIds, CancellationToken ct = default)
    {
        var nonExistingIds = await _hobbyRepository.GetNonExistingIds(hobbyIds, ct);
        if (nonExistingIds.Count > 0)
        {
            throw UserProfileExceptions.InvalidHobbies(nonExistingIds);
        }

        var hobbyStubs = hobbyIds
            .Select(id => new Hobby { Id = id })
            .ToArray();
        foreach (var hobbyStub in hobbyStubs)
        {
            _hobbyRepository.Attach(hobbyStub);
        }
        return hobbyStubs;
    }
}