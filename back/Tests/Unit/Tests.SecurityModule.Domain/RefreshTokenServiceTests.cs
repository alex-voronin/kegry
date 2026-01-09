using AutoFixture.Xunit2;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using SecurityModule.Domain.Aggregates;
using SecurityModule.Domain.Repositories;
using SecurityModule.Domain.Services;
using Shouldly;
using SharedCore.Exceptions;
using Tests.Shared.AutoFixture;

namespace Tests.SecurityModule.Domain;

public class RefreshTokenServiceTests
{
    private const int MaxSimultaneousLoggedInDevices = 10;
    private const int RefreshTokenDurationInDays = 7;

    private readonly User _user = new User()
    {
        Id = Guid.NewGuid(),
        Email = "test@example.com",
        RequiredAction = RequiredAction.None
    };

    [Theory, AutoNSubstituteData]
    internal async Task CreateRefreshTokenAsync_GivenUniqueJti_WhenLimitIsNotReached_ThenReturnValidToken(
        [Frozen] FakeTimeProvider timeProvider,
        [Frozen] IRefreshTokenRepository repository,
        RefreshTokenService service
    )
    {
        timeProvider.AutoAdvanceAmount = TimeSpan.Zero;
        var fixedNow = DateTimeOffset.UtcNow;
        timeProvider.SetUtcNow(fixedNow);
        var jti = Guid.NewGuid();
        repository.GetByJtiAsync(jti.ToString()).Returns((RefreshToken?)null);
        repository.TokensCountAsync(_user.Id).Returns(MaxSimultaneousLoggedInDevices - 1);

        var result = await service.CreateRefreshTokenAsync(
            user: _user,
            jti: jti,
            durationInDays: RefreshTokenDurationInDays,
            maxSimultaneousLoggedInDevices: MaxSimultaneousLoggedInDevices
        );

        result.ShouldNotBeNull();
        result.UserId.ShouldBe(_user.Id);
        result.Jti.ShouldBe(jti.ToString());
        result.Token.ShouldNotBeNullOrWhiteSpace();
        result.CreatedAt.ShouldBe(fixedNow.UtcDateTime);
        result.ExpiresAt.ShouldBe(fixedNow.UtcDateTime.AddDays(RefreshTokenDurationInDays));
    }

    [Theory, AutoNSubstituteData]
    internal async Task CreateRefreshTokenAsync_GivenDuplicateJti_ThenThrows(
        [Frozen] IRefreshTokenRepository repository,
        RefreshTokenService service
    )
    {
        var jti = Guid.NewGuid();
        var existingToken = new RefreshToken(
            userId: _user.Id,
            token: "existing_token",
            jti: jti.ToString(),
            createdAt: default,
            expiresAt: default
        );
        repository.GetByJtiAsync(jti.ToString()).Returns(existingToken);

        var exception = await Should.ThrowAsync<HttpCodeException>(async () =>
            await service.CreateRefreshTokenAsync(
                user: _user,
                jti: jti,
                durationInDays: RefreshTokenDurationInDays,
                maxSimultaneousLoggedInDevices: MaxSimultaneousLoggedInDevices
            )
        );

        exception.ErrorCode.ShouldBe(ErrorCode.AlreadyExists);
    }

    [Theory, AutoNSubstituteData]
    internal async Task CreateRefreshTokenAsync_GivenUniqueJti_WhenLimitIsReached_ThenThrows(
        [Frozen] IRefreshTokenRepository repository,
        RefreshTokenService service
    )
    {
        var jti = Guid.NewGuid();
        repository.GetByJtiAsync(jti.ToString()).Returns((RefreshToken?)null);
        repository.TokensCountAsync(_user.Id).Returns(MaxSimultaneousLoggedInDevices);

        var exception = await Should.ThrowAsync<HttpCodeException>(async () =>
            await service.CreateRefreshTokenAsync(
                user: _user,
                jti: jti,
                durationInDays: RefreshTokenDurationInDays,
                maxSimultaneousLoggedInDevices: MaxSimultaneousLoggedInDevices
            )
        );

        exception.ErrorCode.ShouldBe(ErrorCode.TooManyEntities);
    }
}