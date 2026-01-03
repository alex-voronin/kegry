namespace FeedModule.Application.Services;

public interface IUserActionsService
{
    Task RateUserAsync(Guid myUserId, Guid otherUserId, decimal rating, CancellationToken ct);
    Task BlockUserAsync(Guid myUserId, Guid otherUserId, CancellationToken ct);
    Task SkipUserAsync(Guid myUserId, Guid otherUserId, CancellationToken ct);
    Task InviteUserAsync(Guid myUserId, Guid otherUserId, CancellationToken ct);
}