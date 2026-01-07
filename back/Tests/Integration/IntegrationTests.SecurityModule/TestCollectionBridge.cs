using IntegrationTests.Shared.Setup;

namespace IntegrationTests.SecurityModule;

[CollectionDefinition("IntegrationTests")]
public class TestCollectionBridge : ICollectionFixture<IntegrationWebApplicationFactory>;