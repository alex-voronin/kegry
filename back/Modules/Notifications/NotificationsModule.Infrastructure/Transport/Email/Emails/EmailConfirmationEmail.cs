namespace NotificationsModule.Infrastructure.Transport.Email.Emails;

public class EmailConfirmationEmail : AbstractEmail
{
    public required string ConfirmationCode { get; init; }

    public EmailConfirmationEmail() : base("Confirm your email")
    {
    }

    public override string BuildBody()
    {
        return $"Your confirmation code is {ConfirmationCode}.";
    }
}