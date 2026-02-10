namespace FeedModule.Domain.Services;

internal static class FeedParameters
{
    internal const int FeedLength = 30;
    internal const int UserLimitPerDatabaseQuery = 1000;
    internal const int SkippedUserCutOffInDays = 1;
    internal const int IgnoredUserRatingDowngradeInHours = 1;
    internal const int DefaultAvailableInvitesPerDay = 7;
    internal const double DistanceWeightInScore = 0.19;
    internal const double HobbiesWeightInScore = 0.43;
    internal const double AgeWeightInScore = 0.32;
    internal const double RatingWeightInScore = 0.05;
}