using API.Web;
using API.Web.Exceptions;
using API.Web.Startup;
using Autofac.Extensions.DependencyInjection;
using Security.Setup;
using Serilog;
using Shared.Infrastructure.Interfaces;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

var configuration = builder.Configuration;
IFeatureStartup[] startups =
[
    new SecurityStartup(configuration),
    new SwaggerStartup(),
    new ResponseCompressionStartup()
];
RegisterServices(builder, startups);
StartupHelpers.AwaitDatabaseStartup(configuration: configuration, timeoutSeconds: 30);

var app = builder.Build();
InitializeApp(app, startups);

StartupHelpers.SeedData(app.Services.CreateScope());

app.Run();
return;

static void RegisterServices(WebApplicationBuilder builder, IFeatureStartup[] startups)
{
    foreach (var startup in startups)
    {
        startup.Register(builder, builder.Host);
    }

    builder.Services.AddExceptionHandler<ExceptionHandler>();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();
}

static void InitializeApp(WebApplication app, IFeatureStartup[] startups)
{
    foreach (var startup in startups)
    {
        startup.Initialize(app);
    }

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler(_ => { });
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
}