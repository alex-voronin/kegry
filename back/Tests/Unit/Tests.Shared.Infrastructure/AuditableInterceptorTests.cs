using Shared.Infrastructure.Persistence.Audit;
using SharedCore.Interfaces;

namespace Tests.Shared.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

public class AuditableEntity : IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class NonAuditableEntity
{
    public int Id { get; set; }
    public int Value { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class TestDbContext : DbContext
{
    public DbSet<AuditableEntity> TestEntities { get; set; }
    public DbSet<NonAuditableEntity> NonAuditable { get; set; }

    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
    {
    }
}

public class AuditableInterceptorTests
{
    private readonly DbContextOptions<TestDbContext> _options;

    public AuditableInterceptorTests()
    {
        _options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // every test - clean database
            .AddInterceptors(new AuditableInterceptor())
            .Options;
    }

    private TestDbContext CreateContext() => new TestDbContext(_options);

    [Fact]
    public async Task Save_WhenNewAuditableEntity_ThenSetCreatedAtAndUpdatedAt()
    {
        var entity = new AuditableEntity { Name = "New" };

        await using var ctx = CreateContext();
        ctx.TestEntities.Add(entity);
        await ctx.SaveChangesAsync();

        entity.CreatedAt.ShouldNotBe(default);
        entity.UpdatedAt.ShouldNotBe(default);
        entity.CreatedAt.ShouldBe(entity.UpdatedAt);
    }

    [Fact]
    public async Task Save_WhenAuditableEntityUpdated_ThenUpdatesUpdatedAtButKeepCreatedAt()
    {
        var entity = new AuditableEntity { Name = "Old" };

        await using var ctx = CreateContext();
        ctx.TestEntities.Add(entity);
        await ctx.SaveChangesAsync();

        var created = entity.CreatedAt;
        entity.Name = "Updated";
        await ctx.SaveChangesAsync();

        entity.CreatedAt.ShouldBe(created);
        entity.UpdatedAt.ShouldBeGreaterThan(created);
    }

    [Fact]
    public async Task Save_WhenUpdateAtSetManually_ThenItsValueSaved()
    {
        var entity = new AuditableEntity { Name = "Old" };
        var manualUpdatedAt = DateTimeOffset.UtcNow.AddDays(-5);
        await using var ctx = CreateContext();
        ctx.TestEntities.Add(entity);
        await ctx.SaveChangesAsync();

        entity.Name = "Updated";
        entity.UpdatedAt = manualUpdatedAt;
        await ctx.SaveChangesAsync();

        entity.UpdatedAt.ShouldBe(manualUpdatedAt);
        entity.CreatedAt.ShouldNotBe(default);
    }

    [Fact]
    public async Task Save_WheNonAuditableEntity_ThenNotUpdatesUpdatedAt()
    {
        DateTimeOffset updatedAt = DateTimeOffset.UtcNow.AddDays(-5);
        var entity = new NonAuditableEntity { Value = 42, UpdatedAt = updatedAt };

        await using var ctx = CreateContext();
        ctx.NonAuditable.Add(entity);
        await ctx.SaveChangesAsync();

        entity.UpdatedAt.ShouldBe(updatedAt);
    }
}