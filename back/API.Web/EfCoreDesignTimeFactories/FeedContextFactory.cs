using FeedModule.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace API.Web.EfCoreDesignTimeFactories;

public class FeedContextFactory : EfContextFactoryBase<FeedContext>
{
    protected override void SetupEfCoreExtensions(NpgsqlDbContextOptionsBuilder builder)
    {
        builder.UseNetTopologySuite();
    }
}