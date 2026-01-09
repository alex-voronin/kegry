using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NSubstitute;
using Shared.Infrastructure.Persistence;
using SharedCore.Interfaces;
using Shouldly;

namespace Tests.Shared.Infrastructure;

file class TestDbContextCollection : DbContextCollection
{
    internal HashSet<DbContext> DbContextsWithAppliedTransaction { get; } = new HashSet<DbContext>();
    internal HashSet<IDbContextTransaction> AppliedTransactions { get; } = new HashSet<IDbContextTransaction>();

    internal TestDbContextCollection(RepositoryBase repositoryBase) : base(repositoryBase)
    {
    }

    protected override Task<IDbContextTransaction> BeginTransaction(DbContext dbContext, CancellationToken ct)
    {
        return Task.FromResult(
            Substitute.For<IDbContextTransaction>()
        );
    }

    protected override Task ApplyTransaction(
        DbContext context,
        IDbContextTransaction transaction,
        CancellationToken ct = default)
    {
        DbContextsWithAppliedTransaction.Add(context);
        AppliedTransactions.Add(transaction);
        return Task.CompletedTask;
    }
}

public class DbContextCollectionTests
{
    [Fact]
    public async Task SaveChangesAsync_WhenMultipleRepositories_ThenCallSaveChangesAsyncOnAllContexts()
    {
        var context1 = Substitute.For<DbContext>();
        var repo1 = Substitute.For<RepositoryBase, IRepository>();
        repo1.GetContext().Returns(context1);

        var context2 = Substitute.For<DbContext>();
        var repo2 = Substitute.For<RepositoryBase, IRepository>();
        repo2.GetContext().Returns(context2);

        var collection = new DbContextCollection(repo1).And((IRepository)repo2);

        var ct = CancellationToken.None;

        await collection.SaveChangesAsync(ct);

        await context1.Received(1).SaveChangesAsync(ct);
        await context2.Received(1).SaveChangesAsync(ct);
    }

    [Fact]
    public void SaveChanges_WhenMultipleRepositories_ThenCallSaveChangesOnAllContexts()
    {
        var context1 = Substitute.For<DbContext>();
        var repo1 = Substitute.For<RepositoryBase, IRepository>();
        repo1.GetContext().Returns(context1);

        var context2 = Substitute.For<DbContext>();
        var repo2 = Substitute.For<RepositoryBase, IRepository>();
        repo2.GetContext().Returns(context2);

        var collection = new DbContextCollection(repo1).And((IRepository)repo2);

        collection.SaveChanges();

        context1.Received(1).SaveChanges();
        context2.Received(1).SaveChanges();
    }

    [Fact]
    public async Task StartTransactionAsync_WhenMultipleRepositories_ThenBeginAndUseTheTransactionForAllContexts()
    {
        var context1 = Substitute.For<DbContext>();
        var repo1 = Substitute.For<RepositoryBase, IRepository>();
        repo1.GetContext().Returns(context1);

        var context2 = Substitute.For<DbContext>();
        var repo2 = Substitute.For<RepositoryBase, IRepository>();
        repo2.GetContext().Returns(context2);


        var collection = (TestDbContextCollection)new TestDbContextCollection(repo1).And((IRepository)repo2);
        var result = await collection.StartTransactionAsync();

        result.ShouldNotBeNull();
        collection.DbContextsWithAppliedTransaction.ShouldBe([context1, context2]);
        collection.AppliedTransactions.Count.ShouldBe(1);
    }
}