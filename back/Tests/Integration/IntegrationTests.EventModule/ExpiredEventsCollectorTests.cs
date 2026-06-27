using Amazon.S3;
using Amazon.S3.Model;
using EventModule.Application.BackgroundJobs;
using EventModule.Domain.Aggregates;
using EventModule.Infrastructure.Persistence;
using IntegrationTests.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace IntegrationTests.EventModule;

[Collection("IntegrationTests")]
public class ExpiredEventsCollectorTests : IntegrationTestBase, IAsyncLifetime
{
    public ExpiredEventsCollectorTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => ContainerManager.ResetDatabaseAsync();

    [Fact]
    public async Task GivenEventsOlderThanThreeMonths_WhenCollecting_ThenSoftDeleted()
    {
        var eventId = await SeedEventAsync(
            DateTimeOffset.UtcNow.AddMonths(-(ExpiredEventsCollectorBackgroundService.SoftDeleteAfterMonths + 1)));

        await RunCollectorAsync();

        await AssertEventSoftDeleted(eventId);
    }

    [Fact]
    public async Task GivenEventsOlderThanTwelveMonths_WhenCollecting_ThenHardDeleted()
    {
        var eventId = await SeedEventAsync(
            DateTimeOffset.UtcNow.AddMonths(-(ExpiredEventsCollectorBackgroundService.HardDeleteAfterMonths + 1)));

        await RunCollectorAsync();

        await AssertEventHardDeleted(eventId);
    }

    [Fact]
    public async Task GivenRecentEvents_WhenCollecting_ThenNotDeleted()
    {
        var eventId = await SeedEventAsync(
            DateTimeOffset.UtcNow.AddMonths(-(ExpiredEventsCollectorBackgroundService.SoftDeleteAfterMonths - 2)));

        await RunCollectorAsync();

        await AssertEventNotDeleted(eventId);
    }

    [Fact]
    public async Task GivenAlreadySoftDeletedEventsOlderThanTwelveMonths_WhenCollecting_ThenHardDeleted()
    {
        var eventId = await SeedEventAsync(
            DateTimeOffset.UtcNow.AddMonths(-(ExpiredEventsCollectorBackgroundService.HardDeleteAfterMonths + 1)),
            isDeleted: true);

        await RunCollectorAsync();

        await AssertEventHardDeleted(eventId);
    }

    [Fact]
    public async Task GivenEventsWithS3Keys_WhenCollecting_ThenS3ObjectsAreDeleted()
    {
        var coverKey = $"test-covers/{Guid.NewGuid()}.jpg";
        var eventId = await SeedEventAsync(
            DateTimeOffset.UtcNow.AddMonths(-(ExpiredEventsCollectorBackgroundService.SoftDeleteAfterMonths + 1)),
            coverImageS3Key: coverKey);

        var s3Config = new AmazonS3Config
        {
            ServiceURL = TestConfig.S3.ServiceUrl,
            ForcePathStyle = true
        };
        using var s3Client = new AmazonS3Client(TestConfig.S3.AccessKey, TestConfig.S3.SecretKey, s3Config);

        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = TestConfig.S3.BucketName,
            Key = coverKey,
            ContentBody = "cover-image-content"
        });

        var coverMeta = await s3Client.GetObjectAsync(TestConfig.S3.BucketName, coverKey);
        coverMeta.HttpStatusCode.ShouldBe(System.Net.HttpStatusCode.OK);

        await RunCollectorAsync();

        await AssertEventSoftDeleted(eventId);

        Should.Throw<AmazonS3Exception>(() => s3Client.GetObjectAsync(TestConfig.S3.BucketName, coverKey));
    }

    private async Task<Guid> SeedEventAsync(
        DateTimeOffset startDateTime,
        bool isDeleted = false,
        string? coverImageS3Key = null)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventContext>();

        var eventId = Guid.NewGuid();
        var ev = new Event(
            id: eventId,
            coverImageS3Key: coverImageS3Key,
            title: "Test Event",
            description: "Test Description",
            startDateTime: startDateTime,
            type: global::EventModule.Domain.Enums.EventType.Event,
            venueId: Guid.NewGuid(),
            creatorId: Guid.NewGuid(),
            creatorAvatarS3Key: null,
            maxParticipants: null,
            requiresApproval: false)
        {
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = isDeleted
        };

        context.Events.Add(ev);
        await context.SaveChangesAsync();

        return eventId;
    }

    private async Task RunCollectorAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        var collector = new ExpiredEventsCollectorBackgroundService(scopeFactory);
        await collector.CollectExpiredEvents(CancellationToken.None);
    }

    private async Task AssertEventSoftDeleted(Guid eventId)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventContext>();
        var ev = await context.Events.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == eventId);
        ev.ShouldNotBeNull();
        ev.IsDeleted.ShouldBeTrue();
    }

    private async Task AssertEventHardDeleted(Guid eventId)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventContext>();
        var ev = await context.Events.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == eventId);
        ev.ShouldBeNull();
    }

    private async Task AssertEventNotDeleted(Guid eventId)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventContext>();
        var ev = await context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
        ev.ShouldNotBeNull();
        ev.IsDeleted.ShouldBeFalse();
    }
}
