using System.ComponentModel.DataAnnotations.Schema;

namespace SecurityModule.Domain.Aggregates;

public class RefreshToken
{
    public int Id { get; init; }
    public Guid UserId { get; init; }
    public string Token { get; init; }
    public string Jti { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }

    [NotMapped]
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    [NotMapped]
    public bool IsActive => !IsExpired;

    internal RefreshToken(Guid userId, string token, string jti, DateTime createdAt, DateTime expiresAt)
    {
        UserId = userId;
        Token = token;
        Jti = jti;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }
}