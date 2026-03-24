using IntegrationTests.Shared.Fakes;
using IntegrationTests.Shared.Managers;
using JasperFx.CommandLine;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationsModule.Infrastructure.Transport.Emails;
using NotificationsModule.Infrastructure.Transport.Push;
using NSubstitute;
using IntegrationTests.Shared.HttpClients;
using SecurityModule.Infrastructure.Service;
using PostHog;
using Wolverine;
using Xunit;

namespace IntegrationTests.Shared;

public class IntegrationWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public IntegrationApiClient ApiClient { get; private set; } = null!;
    public HttpClient HttpClient { get; private set; } = null!;
    public S3Client S3Client { get; init; } = new S3Client();
    public AuthManager AuthManager { get; private set; } = null!;
    public NotificationManager NotificationManager { get; private set; } = null!;
    public ResettableTimeProvider FakeTimeProvider { get; init; } = new ResettableTimeProvider(DateTimeOffset.UtcNow);
    public ContainerManager ContainerManager { get; init; } = new ContainerManager();
    public IPostHogClient PostHogClient { get; private set; } = null!;
    public IHost Host { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => { logging.AddConsole(); });

        // connection string must be configured via builder.UseSetting as 
        // AppModuleStartupBase reads connection string in constructor
        builder.UseSetting("ConnectionStrings:hoppyfydb", ContainerManager.DbConnectionString);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:AuthTokenConfig:Secret"] = TestConfig.AuthToken.Secret,
                ["Authentication:AuthTokenConfig:Issuer"] = TestConfig.AuthToken.Issuer,
                ["Authentication:AuthTokenConfig:Audience"] = TestConfig.AuthToken.Audience,
                ["Authentication:AuthTokenConfig:AccessTokenDurationInDays"] =
                    TestConfig.AuthToken.AccessTokenDurationInDays.ToString(),
                ["Authentication:AuthTokenConfig:RefreshTokenDurationInDays"] =
                    TestConfig.AuthToken.RefreshTokenDurationInDays.ToString(),
                ["Authentication:AuthTokenConfig:MaxSimultaneousLoggedInDevices"] =
                    TestConfig.AuthToken.MaxSimultaneousLoggedInDevices.ToString(),
                ["Storage:S3Config:AccessKey"] = TestConfig.S3.AccessKey,
                ["Storage:S3Config:SecretKey"] = TestConfig.S3.SecretKey,
                ["Storage:S3Config:ServiceUrl"] = TestConfig.S3.ServiceUrl,
                ["Storage:S3Config:PublicUrl"] = TestConfig.S3.PublicUrl,
                ["Storage:S3Config:BucketName"] = TestConfig.S3.BucketName,
                ["Storage:S3Config:KeySalt"] = TestConfig.S3.SecretKey,
                ["Storage:S3Config:UseHttp"] = TestConfig.S3.UseHttp.ToString()
            });
        });

        // mocks for notification transports 
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            var emailSender = Substitute.For<IEmailSender>();
            services.AddSingleton(emailSender);

            services.RemoveAll<IPushSender>();
            var pushSender = Substitute.For<IPushSender>();
            pushSender.SendPushMessage(Arg.Any<PushMessage>())
                .Returns(new SendResult(true, []));
            services.AddSingleton(pushSender);

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(FakeTimeProvider);

            services.RemoveAll<IGoogleTokenValidator>();
            services.AddSingleton<IGoogleTokenValidator>(new FakeGoogleTokenValidator());

            services.RemoveAll<IPostHogClient>();
            PostHogClient = Substitute.For<IPostHogClient>();
            services.AddSingleton(PostHogClient);

            NotificationManager = new NotificationManager(emailSender: emailSender, pushSender: pushSender);

            services.ConfigureWolverine(opts => { opts.Durability.ScheduledJobPollingTime = TimeSpan.FromSeconds(1); });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Without this flag WebHost terminates immediately after startup
        JasperFxEnvironment.AutoStartHost = true;
        return base.CreateHost(builder);
    }

    public async Task InitializeAsync()
    {
        await ContainerManager.InitializeAsync();

        Host = Services.GetRequiredService<IHost>();

        var httpClient = CreateClient();
        ApiClient = new IntegrationApiClient(httpClient);
        HttpClient = httpClient;
        AuthManager = new AuthManager(ApiClient, Services, Host);

        await ContainerManager.CreateTestBucketAsync();
        await ContainerManager.InitializeRespawnerAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await ContainerManager.DisposeAsync();
    }
}