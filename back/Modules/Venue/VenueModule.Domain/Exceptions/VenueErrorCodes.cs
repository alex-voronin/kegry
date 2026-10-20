using System.Net;
using SharedCore.Exceptions;

namespace VenueModule.Domain.Exceptions;

/// <summary>
/// Venue-specific client-facing error codes.
/// </summary>
public static class VenueErrorCodes
{
    public static readonly ErrorCode VenueNotFound = new(nameof(VenueNotFound));
    public static readonly ErrorCode PinCheckInDisabled = new(nameof(PinCheckInDisabled));
    public static readonly ErrorCode InvalidPinCode = new(nameof(InvalidPinCode));
    public static readonly ErrorCode OutsideVenueRadius = new(nameof(OutsideVenueRadius));
    public static readonly ErrorCode AlreadyCheckedIn = new(nameof(AlreadyCheckedIn));
    public static readonly ErrorCode CoordinatesRequired = new(nameof(CoordinatesRequired));
}
