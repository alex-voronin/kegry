namespace SharedCore.Interfaces;

public interface IDbContextCollection
{
    public IDbContextCollection And(IRepository repository);
    public void SaveChanges();
    public Task SaveChangesAsync(CancellationToken ct);
    public Task<IMultiRepositoryTransaction> StartTransactionAsync(CancellationToken ct = default);
}