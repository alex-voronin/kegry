namespace FeedModule.Domain.Services;

internal static class FeedScoringParameters
{
    internal const int UserLimitPerDatabaseQuery = 1000;
    internal const int SkippedUserCutOffInDays = 1;
    internal const double DistanceWeightInScoring = 0.2;
    internal const double HobbiesWeightInScoring = 0.45;
    internal const double AgeWeightInScoring = 0.35;
}