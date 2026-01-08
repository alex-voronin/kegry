using IntegrationTests.Shared;
using Xunit;

namespace IntegrationTests.UserProfileModule;

[CollectionDefinition("IntegrationTests")]
public class TestCollectionBridge : ICollectionFixture<IntegrationWebApplicationFactory>;
