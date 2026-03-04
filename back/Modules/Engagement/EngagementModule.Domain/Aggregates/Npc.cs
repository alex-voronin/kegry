namespace EngagementModule.Domain.Aggregates;

public class Npc
{
    public required Guid Id { get; init; }
    public required bool IsSecret { get; init; }
    public required string NameKey { get; init; }
    public required string AvatarUrl { get; init; }
    public required string CoverUrl { get; init; }
    public required string MonologKey { get; init; }
}