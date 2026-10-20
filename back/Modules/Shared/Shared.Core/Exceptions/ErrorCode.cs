namespace SharedCore.Exceptions;

/// <summary>
/// Client-facing error code, serialized as the <c>code</c> extension of ProblemDetails.
/// Generic codes are declared here; modules declare their own codes next to themselves
/// (e.g. VenueErrorCodes). Renaming an existing code is a breaking change for clients.
/// </summary>
public readonly record struct ErrorCode(string Value)
{
    public static readonly ErrorCode GeneralError = new(nameof(GeneralError));
    public static readonly ErrorCode NotFound = new(nameof(NotFound));
    public static readonly ErrorCode DoesNotExist = new(nameof(DoesNotExist));
    public static readonly ErrorCode AlreadyExists = new(nameof(AlreadyExists));
    public static readonly ErrorCode ValidationError = new(nameof(ValidationError));
    public static readonly ErrorCode TooManyEntities = new(nameof(TooManyEntities));
    public static readonly ErrorCode TooManyRequests = new(nameof(TooManyRequests));
    public static readonly ErrorCode NotAllowed = new(nameof(NotAllowed));

    public override string ToString() => Value;
}
