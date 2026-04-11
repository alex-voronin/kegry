namespace FeedModule.Domain.Services;

public interface IUserActionLimitsService
{
    public Task<int> GetAvailableInvitesAsync(Guid userId, TimeZoneInfo userTimeZone, CancellationToken ct = default);
}