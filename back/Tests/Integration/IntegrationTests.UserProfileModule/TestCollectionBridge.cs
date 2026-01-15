using IntegrationTests.Shared;

namespace IntegrationTests.UserProfileModule;

[CollectionDefinition("IntegrationTests")]
public class TestCollectionBridge : ICollectionFixture<IntegrationWebApplicationFactory>;
