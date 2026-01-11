namespace SharedCore.Interfaces;

public interface IMultiRepositoryTransaction
{
    public Task SaveChangesAsync(CancellationToken ct = default);
    public Task CommitAsync(CancellationToken ct = default);
}