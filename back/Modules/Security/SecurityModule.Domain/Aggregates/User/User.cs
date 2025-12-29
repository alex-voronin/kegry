using Microsoft.AspNetCore.Identity;

namespace SecurityModule.Domain.Aggregates.User;

public enum RequiredAction
{
    None,
    EmailVerification,
    OnboardingCompletion,
}

public sealed class User : IdentityUser<Guid>
{
    public required RequiredAction RequiredAction { get; set; }
}