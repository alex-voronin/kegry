using FirebaseAdmin.Messaging;
using Serilog;
using SharedCore.Services.LifeTimes;

namespace NotificationsModule.Infrastructure.Transport.Push;

public record SendResult(bool Success, string[] ExpiredTokens);

public interface IPushSender
{
    public Task<SendResult> SendPushMessage(PushMessage pushMessage);
}

internal class FcmPushSender : IPushSender, ISingleton
{
    private readonly IPushBuilder _pushBuilder;

    public FcmPushSender(IPushBuilder pushBuilder)
    {
        _pushBuilder = pushBuilder;
    }

    public async Task<SendResult> SendPushMessage(PushMessage pushMessage)
    {
        if (pushMessage.UnicastMessage != null)
        {
            return await SendUnicastMessage(pushMessage.UnicastMessage);
        }

        if (pushMessage.MulticastMessage != null)
        {
            return await SendMulticastMessage(pushMessage.MulticastMessage);
        }

        return new SendResult(false, []);
    }

    private async Task<SendResult> SendUnicastMessage(Message message)
    {
        try
        {
            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            Log.Debug("FCM. push has been successfully sent: {0}", response);
            return new SendResult(true, []);
        }
        catch (FirebaseMessagingException e)
        {
            if (e.MessagingErrorCode == MessagingErrorCode.Unregistered)
            {
                return new SendResult(false, [message.Token]);
            }

            throw;
        }
    }

    private async Task<SendResult> SendMulticastMessage(MulticastMessage message)
    {
        var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
        if (response.FailureCount == 0)
        {
            Log.Debug("FCM. multicast push has been successfully sent: {0}", response);
        }

        var expiredTokens = new List<string>();
        for (var i = 0; i < response.Responses.Count; i++)
        {
            if (!response.Responses[i].IsSuccess)
            {
                var exception = response.Responses[i].Exception;
                var failedToken = message.Tokens[i];
                Log.Debug(exception, $"FCM. Multicast sending has failed for: {failedToken}");
                if (exception != null && exception.MessagingErrorCode == MessagingErrorCode.Unregistered)
                {
                    expiredTokens.Add(failedToken);
                }
            }
        }
        return new SendResult(response.FailureCount == 0, expiredTokens.ToArray());
    }
}