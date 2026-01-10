using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Localization;
using NotificationsModule.Infrastructure.Resources;
using SharedCore.Services.LifeTimes;

namespace NotificationsModule.Infrastructure.Transport.Emails;

public record Email(string ToEmail, string Subject, string Body, bool IsImportant);

public interface IEmailBuilder
{
    public Email BuildConfirmEmail(string toEmail, string confirmationCode);
    public Email BuildMeetingArrangedEmail(string toEmail, string otherUserName);
    public Email BuildUserInvitedEmail(string toEmail);
}

internal class EmailBuilder : IEmailBuilder, ISingleton
{
    private readonly IStringLocalizer<NotificationsLocalization> _localizer;

    public EmailBuilder(IStringLocalizer<NotificationsLocalization> localizer)
    {
        _localizer = localizer;
    }


    public Email BuildConfirmEmail(string toEmail, string confirmationCode)
    {
        var title = _localizer.GetString("Email.ConfirmEmail.Subject");
        var body = _localizer.GetString("Email.ConfirmEmail.Body", confirmationCode);
        return new Email(ToEmail: toEmail, Subject: title, Body: body, IsImportant: true);
    }

    public Email BuildMeetingArrangedEmail(string toEmail, string otherUserName)
    {
        var title = _localizer.GetString("Push.MeetingArranged.Title");
        var body = _localizer.GetString("Push.MeetingArranged.Body", otherUserName);
        return new Email(ToEmail: toEmail, Subject: title, Body: body, IsImportant: false);
    }

    public Email BuildUserInvitedEmail(string toEmail)
    {
        var title = _localizer.GetString("Email.UserInvited.Title");
        var body = _localizer.GetString("Email.UserInvited.Body");
        return new Email(ToEmail: toEmail, Subject: title, Body: body, IsImportant: false);
    }
}