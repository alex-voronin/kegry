using SharedCore.Interfaces;

namespace EngagementModule.Domain.Aggregates;

public class User : ISoftDeletable<Guid>
{
    public required Guid Id { get; init; }
    public Guid? ReferrerId { get; init; }
    public bool IsDeleted { get; set; }
    public required DateTimeOffset SeenNpcAt { get; set; }

    private List<Npc> _metNpcs = new();

    public IReadOnlyList<Npc> MetNpcs
    {
        get => _metNpcs;
        init => _metNpcs = value as List<Npc> ?? value.ToList();
    }

    public void AddMetNpc(Npc npc)
    {
        _metNpcs.Add(npc);
    }
}