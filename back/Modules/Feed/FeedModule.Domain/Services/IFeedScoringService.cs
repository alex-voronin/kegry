namespace FeedModule.Domain.Services;

public interface IFeedScoringService
{
    public Task<FeedCandidate[]> GetFeedAsync(
        Guid userId,
        int limit = 30,
        CancellationToken ct = default);
}