using FeedModule.Domain.Aggregates;

namespace FeedModule.Domain.Services;

public interface IUserActionsService
{
    Task RateUserAsync(Guid myUserId, Guid otherUserId, decimal? rating, CancellationToken ct);
    Task BlockUserAsync(Guid myUserId, Guid otherUserId, CancellationToken ct);
    Task UnblockUserAsync(Guid myUserId, Guid otherUserId, CancellationToken ct);
    Task SkipUserAsync(Guid myUserId, Guid otherUserId, CancellationToken ct);
    Task InviteUserAsync(Guid myUserId, Guid otherUserId, CancellationToken ct);
    Task IgnoreUserAsync(Guid myUserId, Guid otherUserId, CancellationToken ct);
    Task<Filter> GetFilterAsync(Guid userId, CancellationToken ct);
    Task UpdateFilterAsync(Guid userId, Filter filter, CancellationToken ct);
}