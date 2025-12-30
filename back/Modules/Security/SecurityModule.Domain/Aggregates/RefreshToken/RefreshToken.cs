using System.ComponentModel.DataAnnotations.Schema;

namespace SecurityModule.Domain.Aggregates.RefreshToken;

public class RefreshToken
{
    public int Id { get; init; }
    public Guid UserId { get; init; }
    public string Token { get; init; }
    public string Jti { get; init; }

    public DateTime Created { get; init; }
    public DateTime Expires { get; init; }

    [NotMapped]
    public bool IsExpired => DateTime.UtcNow >= Expires;

    [NotMapped]
    public bool IsActive => !IsExpired;

    internal RefreshToken(Guid userId, string token, string jti, DateTime created, DateTime expires)
    {
        UserId = userId;
        Token = token;
        Jti = jti;
        Created = created;
        Expires = expires;
    }
}