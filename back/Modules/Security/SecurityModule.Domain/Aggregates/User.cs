using Microsoft.AspNetCore.Identity;
using SecurityModule.Domain.ValueObjects;
using SharedCore.Interfaces;

namespace SecurityModule.Domain.Aggregates;

public enum RequiredAction
{
    None,
    EmailVerification,
    OnboardingCompletion,
}

public sealed class User : IdentityUser<Guid>, IAuditable
{
    public required UserType Type { get; set; }
    public required RequiredAction RequiredAction { get; set; }
    public bool HasGoogleAuth { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}