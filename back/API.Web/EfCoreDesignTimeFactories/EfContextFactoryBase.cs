using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Shared.Setup;

namespace API.Web.EfCoreDesignTimeFactories;

public class EfContextFactoryBase<TContext> : IDesignTimeDbContextFactory<TContext> where TContext : DbContext
{
    public TContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = configuration.GetConnectionString(SharedConstants.DbName);
        Console.WriteLine("Connection string: {0}", connectionString);

        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        optionsBuilder.UseNpgsql(connectionString, SetupEfCoreExtensions);

        return (TContext)Activator.CreateInstance(typeof(TContext), optionsBuilder.Options)!;
    }

    protected virtual void SetupEfCoreExtensions(NpgsqlDbContextOptionsBuilder builder)
    {
    }
}