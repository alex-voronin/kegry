using IntegrationTests.Shared;

namespace IntegrationTests.ChatModule;

[CollectionDefinition("IntegrationTests")]
public class TestCollectionBridge : ICollectionFixture<IntegrationWebApplicationFactory>;
