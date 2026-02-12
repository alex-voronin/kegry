using IntegrationTests.Shared;

namespace IntegrationTests.AnalyticsModule;

[CollectionDefinition("IntegrationTests")]
public class TestCollectionBridge : ICollectionFixture<IntegrationWebApplicationFactory>;
