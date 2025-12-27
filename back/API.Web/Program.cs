using API.Web.Extensions;
using API.Web.Startup;
using Autofac.Extensions.DependencyInjection;
using JasperFx;
using Notifications.Setup;
using Security.Setup;
using Serilog;
using Shared.Infrastructure.Interfaces;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());
builder.UseWolverineInMemoryMessageBus();

var configuration = builder.Configuration;
IFeatureStartup[] startups =
[
    new SecurityModuleStartup(configuration),
    new NotificationsModuleStartup(),
    new SwaggerStartup(),
    new ResponseCompressionStartup()
];
builder.RegisterServices(startups);
builder.AwaitDatabaseStartup( timeoutSeconds: 30);

var app = builder.Build();
app.InitializeApp(startups);
app.SeedData();

return await app.RunJasperFxCommands(args);