using SharedCore.Validation;

namespace FeedModule.Domain.Aggregates;

public class UserRating
{
    public required Guid UserId { get; init; }
    public required int ReviewersCount { get; init; }
    public required decimal Rating { get; init; }

}