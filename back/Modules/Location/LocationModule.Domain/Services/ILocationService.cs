namespace LocationModule.Domain.Services;

public interface ILocationService
{
    public Task UpdateLocation(Guid userId, double newLat, double newLong, CancellationToken ct = default);
}