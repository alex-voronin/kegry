using LocationModule.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace API.Web.EfCoreDesignTimeFactories;

public class LocationContextFactory : EfContextFactoryBase<LocationContext>
{
    protected override void SetupEfCoreExtensions(NpgsqlDbContextOptionsBuilder builder)
    {
        builder.UseNetTopologySuite();
    }
}