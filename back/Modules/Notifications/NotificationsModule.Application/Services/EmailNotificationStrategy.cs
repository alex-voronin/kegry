using NotificationModule.Domain.Aggregates;
using NotificationsModule.Infrastructure.Transport.Emails;
using Serilog;
using SharedCore.Services.LifeTimes;

namespace NotificationsModule.Application.Services;

public interface IEmailNotificationStrategy : INotificationStrategy
{
    public Task SendConfirmEmail(string toEmail, string confirmationCode, CancellationToken ct = default);
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
    
    public async Task SendConfirmEmail(string toEmail, string confirmationCode, CancellationToken ct = default)
    {
        var email = _emailBuilder.BuildConfirmEmail(toEmail: toEmail, confirmationCode: confirmationCode);
        await _emailSender.SendEmailAsync(email, ct);
    }
    
    public async Task SendMeetingArranged(Guid userId, string otherUserName, CancellationToken ct = default)
    {
        await SendEmail(
            userId: userId,
            factory: email => _emailBuilder.BuildMeetingArrangedEmail(email, otherUserName),
            ct: ct);
    }

    public async Task SendUserInvited(Guid userId, CancellationToken ct = default)
    {
        await SendEmail(
            userId: userId,
            factory: email => _emailBuilder.BuildUserInvitedEmail(email),
            ct: ct);
    }
    
    private async Task SendEmail(Guid userId, Func<string, Email> factory,
        CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, asNoTracking:true, ct:ct);
        if (user == null)
        {
            Log.Error("Unable to send an email! User not found {0}", userId);
            return;
        }
       
        var email = factory.Invoke(user.Email);
        await _emailSender.SendEmailAsync(email, ct);
    }
}