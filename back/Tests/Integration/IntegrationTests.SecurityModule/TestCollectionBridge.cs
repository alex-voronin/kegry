using IntegrationTests.Shared;

namespace IntegrationTests.SecurityModule;

[CollectionDefinition("IntegrationTests")]
public class TestCollectionBridge : ICollectionFixture<IntegrationWebApplicationFactory>;