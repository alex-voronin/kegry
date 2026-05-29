namespace EventModule.Domain.Aggregates.ReadOnly;

public class User
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string? AvatarImageS3Key { get; init; }
    public bool IsDeleted { get; init; }

    internal User()
    {
    }
}
