using Microsoft.EntityFrameworkCore.Storage;
using NSubstitute;
using Shared.Infrastructure.Persistence;
using SharedCore.Interfaces;

namespace Tests.Shared.Infrastructure;

public class MultiRepositoryTransactionTests
{
    private readonly IDbContextCollection _dbContextCollection;
    private readonly IDbContextTransaction _internalTransaction;
    private readonly MultiRepositoryTransaction _multiRepositoryTransaction;

    public MultiRepositoryTransactionTests()
    {
        _dbContextCollection = Substitute.For<IDbContextCollection>();
        _internalTransaction = Substitute.For<IDbContextTransaction>();
        _multiRepositoryTransaction = new MultiRepositoryTransaction(_dbContextCollection, _internalTransaction);
    }

    [Fact]
    public async Task SaveChangesAsync_ThenCallCollectionSaveChangesAsync()
    {
        var ct = CancellationToken.None;
        await _multiRepositoryTransaction.SaveChangesAsync(ct);

        await _dbContextCollection.Received(1).SaveChangesAsync(ct);
    }

    [Fact]
    public async Task CommitAsync_WhenNotSaved_ThenCallSaveChangesAsync()
    {
        var ct = CancellationToken.None;
        await _multiRepositoryTransaction.CommitAsync(ct);

        await _dbContextCollection.Received(1).SaveChangesAsync(ct);
        await _internalTransaction.Received(1).CommitAsync(ct);
    }

    [Fact]
    public async Task CommitAsync_WhenAlreadySaved_ThenNotCallSaveChangesAsync()
    {
        var ct = CancellationToken.None;
        await _multiRepositoryTransaction.SaveChangesAsync(ct);
        _dbContextCollection.ClearReceivedCalls();
        await _multiRepositoryTransaction.CommitAsync(ct);

        await _dbContextCollection.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _internalTransaction.Received(1).CommitAsync(ct);
    }
}
