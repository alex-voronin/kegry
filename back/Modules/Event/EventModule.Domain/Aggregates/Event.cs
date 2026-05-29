using System.Diagnostics.CodeAnalysis;
using EventModule.Domain.Enums;
using SharedCore.Interfaces;
using SharedCore.Validation;

namespace EventModule.Domain.Aggregates;

public class Event : IAuditable
{
    private readonly List<EventContact> _eventContacts = [];
    private readonly List<EventHobbyLink> _eventHobbyLinks = [];
    private readonly List<EventParticipant> _eventParticipants = [];

    public required Guid Id { get; init; }

    public string? CoverImageS3Key
    {
        get;
        set => field = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public required string Title
    {
        get;
        set => field = ValidateTitle(value);
    }

    public required string Description
    {
        get;
        set => field = ValidateDescription(value);
    }

    public required DateTimeOffset StartDateTime { get; set; }

    public EventType Type { get; private set; }

    public required Guid VenueId { get; init; }

    public required Guid CreatorId { get; init; }

    public bool IsVisibleOnMap { get; set; }

    public int? MaxParticipants { get; set; }

    public bool RequiresApproval
    {
        get;
        set
        {
            if (Type == EventType.Event && value)
            {
                Validation.ThrowIfInvalid(nameof(RequiresApproval),
                    () => false,
                    "RequiresApproval can only be set for meetups");
            }
            field = value;
        }
    }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public IReadOnlyCollection<EventContact> EventContacts => _eventContacts;
    public IReadOnlyCollection<EventHobbyLink> EventHobbyLinks => _eventHobbyLinks;
    public IReadOnlyCollection<EventParticipant> EventParticipants => _eventParticipants;

    internal Event()
    {
    }

    [SetsRequiredMembers]
    public Event(
        Guid id,
        string? coverImageS3Key,
        string title,
        string description,
        DateTimeOffset startDateTime,
        EventType type,
        Guid venueId,
        Guid creatorId,
        int? maxParticipants,
        bool requiresApproval)
    {
        Id = id;
        Type = type;
        _eventContacts = [];
        _eventHobbyLinks = [];
        _eventParticipants = [];

        CoverImageS3Key = coverImageS3Key;
        Title = title;
        Description = description;
        StartDateTime = startDateTime;
        VenueId = venueId;
        CreatorId = creatorId;
        IsVisibleOnMap = true;
        MaxParticipants = maxParticipants;

        if (type == EventType.Event && requiresApproval)
        {
            Validation.ThrowIfInvalid(nameof(RequiresApproval),
                () => false,
                "RequiresApproval can only be set for meetups");
        }
        RequiresApproval = requiresApproval;
    }

    public void UpdateDetails(
        string? coverImageS3Key,
        string title,
        string description,
        DateTimeOffset startDateTime,
        bool isVisibleOnMap,
        int? maxParticipants,
        bool requiresApproval)
    {
        CoverImageS3Key = coverImageS3Key;
        Title = title;
        Description = description;
        StartDateTime = startDateTime;
        IsVisibleOnMap = isVisibleOnMap;
        MaxParticipants = maxParticipants;
        RequiresApproval = requiresApproval;
    }

    public void SetContacts(IReadOnlyCollection<EventContact> contacts)
    {
        _eventContacts.Clear();
        _eventContacts.AddRange(contacts);
    }

    public void SetHobbyLinks(IReadOnlyCollection<EventHobbyLink> links)
    {
        _eventHobbyLinks.Clear();
        _eventHobbyLinks.AddRange(links);
    }

    public void AddParticipant(EventParticipant participant)
    {
        if (_eventParticipants.Any(p => p.UserId == participant.UserId))
        {
            return;
        }

        if (MaxParticipants.HasValue && _eventParticipants.Count(p => p.Status == ParticipantStatus.Approved || p.Status == ParticipantStatus.Pending) >= MaxParticipants.Value)
        {
            Validation.ThrowIfInvalid(nameof(EventParticipants),
                () => false,
                "Event has reached maximum participants");
        }

        _eventParticipants.Add(participant);
    }

    public void RemoveParticipant(Guid userId)
    {
        var participant = _eventParticipants.FirstOrDefault(p => p.UserId == userId);
        if (participant != null)
        {
            _eventParticipants.Remove(participant);
        }
    }

    public EventParticipant? GetParticipant(Guid userId)
    {
        return _eventParticipants.FirstOrDefault(p => p.UserId == userId);
    }

    private static string ValidateTitle(string value)
    {
        Validation.ThrowIfNullOrWhiteSpace(nameof(Title), value);
        Validation.ThrowIfLengthExceeded(nameof(Title), value, 200);
        return value.Trim();
    }

    private static string ValidateDescription(string value)
    {
        Validation.ThrowIfNullOrWhiteSpace(nameof(Description), value);
        Validation.ThrowIfLengthExceeded(nameof(Description), value, 2000);
        return value.Trim();
    }
}
