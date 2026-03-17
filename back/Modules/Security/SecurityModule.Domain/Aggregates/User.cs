using Microsoft.AspNetCore.Identity;
using SecurityModule.Domain.ValueObjects;

namespace SecurityModule.Domain.Aggregates;

public enum RequiredAction
{
    None,
    EmailVerification,
    OnboardingCompletion,
}

public sealed class User : IdentityUser<Guid>
{
    public required UserType Type { get; init; }
    public required RequiredAction RequiredAction { get; set; }
    public bool HasGoogleAuth { get; set; }
}