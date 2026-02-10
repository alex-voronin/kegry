using FeedModule.Domain.Aggregates;
using SharedCore.Exceptions;
using Shouldly;

namespace Tests.FeedModule.Domain;

public class FeedUserRelationTests
{
    private FeedUserRelation CreateFeedUserRelation()
    {
        return new FeedUserRelation
        {
            Id = Guid.NewGuid(),
            MyUserId = Guid.NewGuid(),
            OtherUserId = Guid.NewGuid(),
            InviteExpiresAt = DateTimeOffset.UtcNow,
            SkippedAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow,
            InvitedAt = DateTimeOffset.UtcNow
        };
    }

    [Fact]
    public void GivenValidReportMessage_WhenSetting_ThenNoExceptionIsThrown()
    {
        var relation = CreateFeedUserRelation();
        var message = new string('a', ValidationParameters.MaxReportMessageLength);
        var act = () => relation.ReportMessage = message;
        act.ShouldNotThrow();
        relation.ReportMessage.ShouldBe(message);
    }

    [Fact]
    public void GivenReportMessageExceedingLength_WhenSetting_ThenValidationExceptionIsThrown()
    {
        var relation = CreateFeedUserRelation();
        var message = new string('a', ValidationParameters.MaxReportMessageLength + 1);
        var act = () => relation.ReportMessage = message;
        act.ShouldThrow<HttpCodeException>();
    }

    [Fact]
    public void GivenNullReportMessage_WhenSetting_ThenNoExceptionIsThrown()
    {
        var relation = CreateFeedUserRelation();
        var act = () => relation.ReportMessage = null;
        act.ShouldNotThrow();
        relation.ReportMessage.ShouldBeNull();
    }
}
