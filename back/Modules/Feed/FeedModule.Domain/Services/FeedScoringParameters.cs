namespace FeedModule.Domain.Services;

internal static class FeedScoringParameters
{
    internal const int UserLimitPerDatabaseQuery = 1000;
    internal const int SkippedUserCutOffInDays = 1;
    internal const double DistanceWeightInScoring = 1.0;
    internal const double HobbiesWeightInScoring = 2.5;
    internal const double AgeWeightInScoring = 2.0;
}