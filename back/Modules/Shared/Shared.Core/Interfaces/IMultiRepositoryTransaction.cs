namespace SharedCore.Interfaces;

public interface IMultiRepositoryTransaction
{
    public Task SaveChangesAsync(CancellationToken ct);
    public Task CommitAsync(CancellationToken ct);
}