using IntegrationTests.Shared;

namespace IntegrationTests.EngagementModule;

[CollectionDefinition("IntegrationTests")]
public class TestCollectionBridge : ICollectionFixture<IntegrationWebApplicationFactory>;
