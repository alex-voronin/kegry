using IntegrationTests.Shared;

namespace IntegrationTests.EventModule;

[CollectionDefinition("IntegrationTests")]
public class TestCollectionBridge : ICollectionFixture<IntegrationWebApplicationFactory>;
