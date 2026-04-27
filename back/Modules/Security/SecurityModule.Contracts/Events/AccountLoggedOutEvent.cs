using SharedCore.Events;

namespace SecurityModule.Contracts.Events;

public record AccountLoggedOutEvent : UserEvent
{
    public required string? DeviceId { get; init; }
}