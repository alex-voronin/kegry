using System.ComponentModel.DataAnnotations.Schema;
using SharedCore.ValueObjects;

namespace FeedModule.Domain.Aggregates.ReadOnly;

public class User
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string AvatarImageS3Key { get; init; } = null!;
    public string CoverImageS3Key { get; init; } = null!;
    public DateOnly Birthday { get; init; }
    public Gender Gender { get; init; }
    public bool IsDeleted { get; set; }

    public IReadOnlyList<Hobby> Hobbies { get; init; } = new List<Hobby>();

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    internal User()
    {
    }
}