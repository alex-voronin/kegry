using IntegrationTests.Shared;

namespace IntegrationTests.NotificationsModule;

[CollectionDefinition("IntegrationTests")]
public class TestCollectionBridge : ICollectionFixture<IntegrationWebApplicationFactory>;