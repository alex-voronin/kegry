using System.Text.RegularExpressions;
using EventModule.Domain.Enums;
using SharedCore.Validation;

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
        Value = ValidateValue(type, value);
        IsPublic = isPublic;
    }

    private static string ValidateValue(ContactType type, string value)
    {
        Validation.ThrowIfNullOrWhiteSpace(nameof(Value), value);

        switch (type)
        {
            case ContactType.Telegram:
                Validation.ThrowIfInvalid(nameof(Value),
                    () => Regex.IsMatch(value, @"^[a-zA-Z0-9_]{1,32}$"),
                    "Telegram username must be 1-32 characters, alphanumeric and underscore only");
                break;

            case ContactType.Viber:
            case ContactType.WhatsApp:
            case ContactType.Phone:
                Validation.ThrowIfInvalid(nameof(Value),
                    () => value.Length >= 4 && value.Length <= 20 && Regex.IsMatch(value, @"^\+?\d+$"),
                    $"{type} must be a valid phone number (4-20 digits, may start with +)");
                break;

            case ContactType.Email:
                Validation.ThrowIfInvalidEmail(nameof(Value), value);
                break;
        }

        return value;
    }
}
