using API.Web.Extensions;
using API.Web.Startup;
using JasperFx;
using LocationModule.Setup;
using NotificationsModule.Setup;
using SecurityModule.Setup;
using UserProfileModule.Setup;
using Serilog;
using Shared.Infrastructure.Interfaces;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

var configuration = builder.Configuration;
IFeatureStartup[] startups =
[
    new SecurityModuleStartup(configuration),
    new UserProfileModuleStartup(configuration),
    new LocationModuleStartup(configuration),
    new NotificationsModuleStartup(),
    new HttpClientsResilienceStartup(),
    new SwaggerStartup(),
    new ResponseCompressionStartup(),
    // wolverine must be registered after all modules to properly manage DI in handlers
    new WolverineBusStartup()
];
builder.RegisterServices(startups);
builder.AwaitDatabaseStartup( timeoutSeconds: 30);

var app = builder.Build();
app.InitializeApp(startups);
app.SeedData();

return await app.RunJasperFxCommands(args);