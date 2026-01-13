using FeedModule.Domain.Aggregates.ReadOnly;

namespace FeedModule.Domain.Services;

public sealed class FeedCandidate
{
    public required User User { get; init; }
    public required decimal? Rating { get; init; }
    public required int DistanceMeters { get; init; }
}