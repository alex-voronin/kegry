namespace EventModule.Domain.Aggregates.ReadOnly;

public class Company
{
    public Guid Id { get; init; }
    public Guid OwnerId { get; init; }
    public string Name { get; init; } = null!;
    public bool IsDeleted { get; init; }

    internal Company()
    {
    }
}
