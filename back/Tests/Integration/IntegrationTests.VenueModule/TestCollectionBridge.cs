using IntegrationTests.Shared;

namespace IntegrationTests.VenueModule;

[CollectionDefinition("IntegrationTests")]
public class TestCollectionBridge : ICollectionFixture<IntegrationWebApplicationFactory>;
