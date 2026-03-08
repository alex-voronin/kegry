using ChatModule.Contracts.Events;
using NotificationModule.Domain.Aggregates;
using NotificationModule.Domain.Repositories;
using NotificationsModule.Infrastructure.Transport.Emails;
using Serilog;
using SharedCore.Services.LifeTimes;

namespace NotificationsModule.Application.Services;

public interface IEmailNotificationStrategy : INotificationStrategy
{
    public Task SendConfirmEmail(string toEmail, string confirmationCode, CancellationToken ct = default);
    public Task SendPasswordReset(string toEmail, string resetToken, CancellationToken ct = default);
}

public class EmailNotificationStrategy : IEmailNotificationStrategy, IScoped
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailBuilder _emailBuilder;
    private readonly IEmailSender _emailSender;

    public EmailNotificationStrategy(
        IUserRepository userRepository,
        IEmailBuilder emailBuilder,
        IEmailSender emailSender)
    {
        _userRepository = userRepository;
        _emailBuilder = emailBuilder;
        _emailSender = emailSender;
    }

    public Task SendConfirmEmail(string toEmail, string confirmationCode, CancellationToken ct = default)
    {
        var email = _emailBuilder.BuildConfirmEmail(toEmail: toEmail, confirmationCode: confirmationCode);
        return _emailSender.SendEmailAsync(email, ct);
    }

    public Task SendPasswordReset(string toEmail, string resetToken, CancellationToken ct = default)
    {
        var email = _emailBuilder.BuildPasswordResetEmail(toEmail: toEmail, resetToken: resetToken);
        return _emailSender.SendEmailAsync(email, ct);
    }

    public Task SendMeetingArranged(Guid userId, string otherUserFullName, CancellationToken ct = default)
    {
        return SendEmail(
            userId: userId,
            factory: email => _emailBuilder.BuildMeetingArrangedEmail(email, otherUserFullName),
            ct: ct);
    }

    public Task SendUserInvited(Guid userId, CancellationToken ct = default)
    {
        return SendEmail(
            userId: userId,
            factory: email => _emailBuilder.BuildUserInvitedEmail(email),
            ct: ct);
    }

    public Task SendChatMessageReceived(ChatMessageSentEvent @event, CancellationToken ct = default)
    {
        return SendEmail(
            userId: @event.ToUserId,
            factory: email => _emailBuilder.BuildChatMessageReceivedEmail(email, @event),
            ct: ct);
    }

    public Task SendReferrerRewardReceived(Guid userId, CancellationToken ct = default)
    {
        return SendEmail(
            userId: userId,
            factory: email => _emailBuilder.BuildReferrerRewardReceived(email),
            ct: ct);
    }

    private async Task SendEmail(Guid userId, Func<string, Email> factory,
        CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, asNoTracking: true, ct: ct);
        if (user == null)
        {
            Log.Error("Unable to send an email! User not found {0}", userId);
            return;
        }

        var email = factory.Invoke(user.Email);
        await _emailSender.SendEmailAsync(email, ct);
    }
}