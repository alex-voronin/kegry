using IntegrationTests.Shared.Setup;
using Xunit;

namespace IntegrationTests.FeedModule;

[CollectionDefinition("IntegrationTests")]
public class TestCollectionBridge : ICollectionFixture<IntegrationWebApplicationFactory>;
