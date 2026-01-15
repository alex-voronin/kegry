using IntegrationTests.Shared;

namespace IntegrationTests.FeedModule;

[CollectionDefinition("IntegrationTests")]
public class TestCollectionBridge : ICollectionFixture<IntegrationWebApplicationFactory>;
