using SharedCore.ValueObjects;

namespace FeedModule.Domain.Aggregates.ReadOnly;

public class UserProfile
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string CoverImageUrl { get; init; } = null!;
    public DateOnly Birthday { get; init; }
    public required Gender Gender { get; set; }
    
    public ICollection<Hobby> Hobbies { get; init; } = new List<Hobby>();

    internal UserProfile()
    {
    }
}