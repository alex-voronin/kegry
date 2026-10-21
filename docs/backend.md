# Backend

## Stack

- .NET 10, ASP.NET Core minimal APIs
- WolverineFX for messaging
- EF Core with PostgreSQL/PostGIS
- xUnit, Shouldly, AutoFixture, NSubstitute for tests

## Startup

`back/API.Web/Program.cs` composes the application from an ordered array of `IFeatureStartup` implementations. Each module registers its own services and initialization logic. Wolverine is registered last so it can see all handlers.

Endpoint classes implement `IEndpoints`; they are discovered and mapped automatically under `/api/v1`.

## Modules

There are 12 folders in `back/Modules/`: 11 business modules plus `Shared`.

`AdminPanel`, `Analytics`, `Chat`, `Engagement`, `Event`, `Feed`, `Location`, `Notifications`, `Security`, `UserProfile`, `Venue`, `Shared`.

Most modules follow this layout:

```
{Module}Module.API             HTTP endpoints and DTOs
{Module}Module.Application     use cases, handlers, application services
{Module}Module.Domain          aggregates, value objects, domain services
{Module}Module.Infrastructure  EF Core persistence, external integrations
{Module}Module.Contracts       messages/events shared with other modules
{Module}Module.Setup           DI registration and startup wiring
```

Exceptions:

- `AdminPanel` — `AdminPanelModule.RazorPages` + `AdminPanelModule.Setup`
- `Analytics` — `AnalyticsModule.Application` + `AnalyticsModule.Setup`
- `Notifications` — `NotificationsModule.API`, `.Application`, `.Infrastructure`, `.Setup` and `NotificationModule.Domain`; no `Contracts`
- `Venue` — `VenueModule.API`, `.Domain`, `.Infrastructure`, `.Setup`; no `Application` or `Contracts`
- `Shared` — `Shared.Core`, `Shared.Infrastructure`, `Shared.Setup`

Modules communicate through Wolverine messages. Durable local queues are stored in PostgreSQL.

## Database

- Modules with persistence have their own EF Core `DbContext`.
- PostgreSQL with PostGIS; snake_case naming conventions.
- Migrations are applied automatically on startup.
- Add a migration with `make add-migration-back module=<Module> name=<Name>`.
- Tests use the same schema and reset the database with Respawn after each test.

## API

- Minimal APIs, one endpoint class per feature area.
- Routes live under `/api/v1`.
- Authentication uses JWT bearer tokens.
- Errors are returned as Problem Details.

## Tests

- Unit tests: xUnit + Shouldly + AutoFixture + NSubstitute.
- Integration tests: `WebApplicationFactory`, Testcontainers (PostGIS and MinIO), and Respawn.
- `make run-tests` runs `dotnet test -m:1`. Projects run sequentially to avoid fixed-port conflicts (54321 for PostgreSQL, 9000 for S3).
- Docker must be running for integration tests.

## Development

- `make run-back` starts the API on port 5214.
- `make build-back` builds the solution.
- The port comes from `Kestrel:Endpoints:Http:Url` in `appsettings.Development.json`; `launchSettings.json` also defines 5222, but the Kestrel configuration takes precedence.
