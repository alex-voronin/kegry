namespace FeedModule.Domain.Services;

public interface IUserActionLimitsService
{
    Task<int> GetAvailableInvitesAsync(Guid userId, CancellationToken ct = default);
}