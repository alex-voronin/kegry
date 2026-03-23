using System.Text;
using ChatModule.Contracts.Events;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using NotificationsModule.Infrastructure.Resources;
using SharedCore.Services.LifeTimes;

namespace NotificationsModule.Infrastructure.Transport.Emails;

public record Email(string ToEmail, string Subject, string Body, bool IsImportant);

public interface IEmailBuilder
{
    public Email BuildConfirmEmail(string toEmail, string confirmationCode);
    public Email BuildMeetingArrangedEmail(string toEmail, string otherUserFullName);
    public Email BuildUserInvitedEmail(string toEmail);
    public Email BuildChatMessageReceivedEmail(string toEmail, ChatMessageSentEvent @event);
    public Email BuildPasswordResetEmail(string toEmail, string baseUrl, string resetToken);
    public Email BuildReferrerRewardReceived(string toEmail);
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

    public Email BuildMeetingArrangedEmail(string toEmail, string otherUserFullName)
    {
        var title = _localizer.GetString("Push.MeetingArranged.Title");
        var body = _localizer.GetString("Push.MeetingArranged.Body", otherUserFullName);
        return new Email(ToEmail: toEmail, Subject: title, Body: body, IsImportant: false);
    }

    public Email BuildUserInvitedEmail(string toEmail)
    {
        var title = _localizer.GetString("Email.UserInvited.Title");
        var body = _localizer.GetString("Email.UserInvited.Body");
        return new Email(ToEmail: toEmail, Subject: title, Body: body, IsImportant: false);
    }

    public Email BuildChatMessageReceivedEmail(string toEmail, ChatMessageSentEvent @event)
    {
        var title = _localizer.GetString("Email.ChatMessageReceived.Title");
        var body = _localizer.GetString("Email.ChatMessageReceived.Body", @event.FromUserName);
        return new Email(ToEmail: toEmail, Subject: title, Body: body, IsImportant: false);
    }

    public Email BuildPasswordResetEmail(string toEmail, string baseUrl, string resetToken)
    {
        var encodedEmail = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(toEmail));
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetToken));
        var title = _localizer.GetString("Email.PasswordReset.Subject");
        var body = _localizer.GetString("Email.PasswordReset.Body", baseUrl, encodedEmail, encodedToken);
        return new Email(ToEmail: toEmail, Subject: title, Body: body, IsImportant: true);
    }

    public Email BuildReferrerRewardReceived(string toEmail)
    {
        var title = _localizer.GetString("Email.ReferrerRewardReceived.Title");
        // TODO: add friend's name in the body
        var body = _localizer.GetString("Email.ReferrerRewardReceived.Body");
        return new Email(ToEmail: toEmail, Subject: title, Body: body, IsImportant: false);
    }
}