namespace SharedCore.Interfaces;

public interface ISoftDeletable<TId>
{
    TId Id { get; init; }
    bool IsDeleted { get; set; }
}