using hoppyfy.me.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var postgresUser = Environment.GetEnvironmentVariable("HOPPYFY_POSTGRES_USER")!;
var postgresPassword = Environment.GetEnvironmentVariable("HOPPYFY_POSTGRES_PASSWORD")!;
var postgresDataDirectory = Environment.GetEnvironmentVariable("HOPPYFY_POSTGRES_DATA_DIRECTORY")!;

var usernameParam = builder.AddParameter("username", postgresUser!);
var passwordParam = builder.AddParameter("password", postgresPassword!);

var postgres = builder
    .AddPostgres("postgres", usernameParam, passwordParam)
    .WithImageTag(DockerContainers.PostgresVersion);
// .WithBindMount(postgresDataDirectory, "/var/lib/postgresql", false); // TODO: uncomment

// TODO: find a way to share a constant with the database name between Aspire project and main backend 
var db = postgres.AddDatabase("kegrydb");

builder.AddProject<Projects.API_Web>("hoppyfy-me-api")
    .WithReference(db);

builder.Build().Run();