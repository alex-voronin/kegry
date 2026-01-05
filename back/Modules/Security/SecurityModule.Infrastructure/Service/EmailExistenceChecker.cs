using DnsClient;
using SharedCore.Services.LifeTimes;

namespace SecurityModule.Infrastructure.Service;

public interface IEmailExistenceChecker
{
    public Task<bool> CheckDomainExists(string email);
}

internal class EmailExistenceChecker : IEmailExistenceChecker, ISingleton
{
    public async Task<bool> CheckDomainExists(string email)
    {
        var domain = email.Split('@').Last();
        var lookup = new LookupClient();
        var result = await lookup.QueryAsync(domain, QueryType.MX);
        return result.Answers.MxRecords().Any();
    }
}