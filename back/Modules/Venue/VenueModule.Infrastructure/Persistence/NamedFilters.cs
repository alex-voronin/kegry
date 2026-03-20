namespace VenueModule.Infrastructure.Persistence;

internal static class NamedFilters
{
    public static class Venue
    {
        public const string Verified = "VerifiedFilter";
        public const string SoftDeleted = "SoftDeletedFilter";
    }
}