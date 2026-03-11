using EngagementModule.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using VenueModule.Infrastructure.Persistence;

namespace API.Web.EfCoreDesignTimeFactories;

public class VenueContextFactory : EfContextFactoryBase<VenueContext>
{
    protected override void SetupEfCoreExtensions(NpgsqlDbContextOptionsBuilder builder)
    {
        builder.UseNetTopologySuite();
    }
}