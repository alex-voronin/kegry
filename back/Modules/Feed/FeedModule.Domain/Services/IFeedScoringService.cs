namespace FeedModule.Domain.Services;

public interface IFeedScoringService
{
    public Task<FeedCandidate[]> GetFeedAsync(
        Guid userId,
        CancellationToken ct);
}