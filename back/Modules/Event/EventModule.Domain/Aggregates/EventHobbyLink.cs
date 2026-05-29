namespace EventModule.Domain.Aggregates;

public class EventHobbyLink
{
    public Guid EventId { get; init; }
    public Guid HobbyId { get; init; }

    internal EventHobbyLink()
    {
    }

    public EventHobbyLink(Guid eventId, Guid hobbyId)
    {
        EventId = eventId;
        HobbyId = hobbyId;
    }
}
