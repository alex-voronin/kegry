using EventModule.Domain.Enums;

namespace EventModule.Domain.Aggregates;

public class EventContact
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public ContactType Type { get; set; }
    public string Value { get; set; } = null!;
    public bool IsPublic { get; set; }

    internal EventContact()
    {
    }

    public EventContact(Guid id, Guid eventId, ContactType type, string value, bool isPublic)
    {
        Id = id;
        EventId = eventId;
        Type = type;
        Value = value;
        IsPublic = isPublic;
    }
}
