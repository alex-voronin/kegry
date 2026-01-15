using IntegrationTests.Shared.Managers;

namespace IntegrationTests.Shared;

using System;

public abstract class IntegrationTestBase
{
    protected readonly ContainerManager ContainerManager;
    protected readonly IntegrationWebApplicationFactory Factory;

    protected IntegrationTestBase(IntegrationWebApplicationFactory factory)
    {
        Factory = factory;
        ContainerManager = factory.ContainerManager;
    }
}