namespace UserProfileModule.Domain.Aggregates;

public class HobbyCategory
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;

    public List<Hobby> Hobbies { get; init; } = new();
}
