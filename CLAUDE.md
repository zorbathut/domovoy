# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Wumpus is a telemetry and error tracking system for games built with C# and ASP.NET Core 9. It collects and visualizes two types of reports:
- **Events** - Game analytics and custom events (e.g., level completions, user actions)
- **Errors** - Error conditions and crashes with stack traces

The system consists of multiple services that work together to collect, store, and visualize these reports.

## Development Philosophy

This project follows MVP/KISS principles (Minimum Viable Product / Keep It Simple, Stupid). When making changes or adding features:
- Include what's necessary, not more
- Avoid over-engineering or adding speculative features
- Keep implementations straightforward and maintainable

## Solution Structure

The solution contains 5 projects:

- **Wumpus.Shared** - Shared models (`Report` base class, `Event` and `Error` types) and DTOs used across all projects
- **Wumpus.Database** - EF Core DbContext, migrations, and database configuration
- **Wumpus.Intake** - ASP.NET Core Web API that receives events and errors via HTTP
- **Wumpus.Web** - Blazor Server web interface for viewing events and errors
- **Wumpus.Client** - Client library for games to send events and errors to the Intake API

## Common Development Commands

### Building
```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/Wumpus.Intake/Wumpus.Intake.csproj
```

### Running Services

**Local development (services outside Docker):**
```bash
# Start PostgreSQL only
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up postgres

# Run Intake API (in separate terminal)
dotnet run --project src/Wumpus.Intake/Wumpus.Intake.csproj

# Run Web UI (in separate terminal)
dotnet run --project src/Wumpus.Web/Wumpus.Web.csproj
```

**Full Docker deployment:**
```bash
docker-compose up
```

### Database Migrations

Migrations are automatically applied on startup by both Intake and Web services (see Program.cs in each project).

To create a new migration:
```bash
# Must be run from the Wumpus.Database directory
cd src/Wumpus.Database
dotnet ef migrations add MigrationName
```

To manually apply migrations:
```bash
cd src/Wumpus.Database
dotnet ef database update
```

### Testing

**Prerequisites:** PostgreSQL must be running (via docker-compose) before running tests.

```bash
# Start PostgreSQL for tests
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up postgres

# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~IntakeApiTests"
```

#### Test Project Structure

The solution includes a single test project `Wumpus.Tests` that contains integration tests for all components:

- **IntakeApiTests** - Tests the Intake API HTTP endpoints and database persistence
- **ReportServiceTests** - Tests the ReportService directly, including event and error creation
- **WumpusClientTests** - Tests the client library's end-to-end integration with the Intake API
- **WebUiTests** - Tests Blazor components using bUnit, verifying UI rendering and data display

#### Test Database

Tests use a separate `wumpus_test` database to avoid interfering with development data:

- The `DatabaseFixture` (in `tests/Wumpus.Tests/Infrastructure/DatabaseFixture.cs`) manages the test database lifecycle
- The test database is dropped and recreated before each test run to ensure clean state
- Tests clean up after themselves using `IAsyncLifetime.DisposeAsync()`
- Connection string: `Host=localhost;Database=wumpus_test;Username=wumpus;Password=wumpus`

#### Test Infrastructure

The test project includes several infrastructure components:

- **IntakeApiFactory** - Custom `WebApplicationFactory` for testing the Intake API
- **WebUiFactory** - Custom `WebApplicationFactory` for testing the Web UI
- **DatabaseFixture** - Manages test database creation, cleanup, and provides helper methods
- **TestDataBuilder** - Fluent builder for creating test event and error report data

#### What the Tests Cover

1. **API Integration** - Full HTTP request/response cycle including serialization, validation, and error handling
2. **Database Operations** - Actual PostgreSQL queries, migrations, and data persistence
3. **Report Storage** - Each event and error report is stored as a unique record (no deduplication)
4. **Client Library** - End-to-end event and error submission from client to database
5. **UI Components** - Blazor component rendering, data binding, and user interactions

#### Running Tests Locally

1. Ensure PostgreSQL is running: `docker-compose -f docker-compose.yml -f docker-compose.dev.yml up postgres`
2. Run tests: `dotnet test`
3. The first test run will create the `wumpus_test` database automatically
4. Each test class cleans up after itself, so tests can be run repeatedly

**Note**: Tests are configured to run sequentially (not in parallel) via `xunit.runner.json` because they share a single test database. This ensures reliability but makes tests slightly slower (~2-4 seconds total).

## Architecture Details

### Service Communication

- **Intake API** (port 5001) and **Web UI** (port 5000) do NOT communicate with each other
- Both services independently connect to the same PostgreSQL database
- Events and errors submitted to the Intake API are stored in the database and retrieved by the Web UI

### Data Model Design

The system uses a Table-Per-Hierarchy (TPH) pattern with an abstract `Report` base class and two concrete types:

**Report** (abstract base class, table: Reports)
- `Id` (Guid, PK)
- `Timestamp` (DateTime)
- `ReportType` (enum discriminator: Event=1, Error=2)
- `GameVersion` (string, max 50 chars)
- `Platform` (string, max 50 chars)

**Event : Report** (ReportType = Event)
- `Data` (owned entity of type `EventData`) contains:
  - `Name` (string, max 200 chars) - Event name
  - `Category` (string, max 100 chars) - Event category
  - `Value` (decimal?) - Optional numeric value
  - `UserId` (string?, max 100 chars) - Optional user identifier
  - `Metadata` (Dictionary<string, object>?) - Optional JSONB metadata

**Error : Report** (ReportType = Error)
- `Data` (owned entity of type `ErrorData`) contains:
  - `Severity` (string, max 20 chars) - Error severity level
  - `Code` (string?, max 100 chars) - Optional error code
  - `Message` (string, max 2000 chars) - Error message
  - `ExceptionType` (string?, max 500 chars) - Exception type
  - `StackTrace` (string?) - Full stack trace
  - `Context` (string?) - Additional context

The owned entity pattern means owned entity fields are stored as columns in the Reports table using the `Data_` prefix (e.g., `Data_Name`, `Data_Severity`). Partial indexes on `Data_UserId` (for events) and `Data_Severity` (for errors) optimize queries for each type. See `WumpusDbContext.cs` for the EF Core configuration.

**Important**: Each submission creates a new record - there is NO deduplication. Every event and error is stored individually.

### Connection String Configuration

- **Development (local)**: `Host=localhost;Database=wumpus;Username=wumpus;Password=wumpus`
- **Docker**: `Host=postgres;Database=wumpus;Username=wumpus;Password=wumpus`
- Configured in `appsettings.json` (or `appsettings.Development.json`) in both Intake and Web projects
- Docker services use environment variable `ConnectionStrings__DefaultConnection` (see docker-compose.yml)

### Database Design Time Factory

`WumpusDbContextFactory.cs` provides a design-time factory for EF Core migrations. It uses a hardcoded localhost connection string that is only used during `dotnet ef migrations add` commands, not at runtime.

## Service Ports

- **Web UI**: 5000
- **Intake API**: 5001
- **PostgreSQL**: 5432

Both Intake and Web expose `/health` endpoints for health checks.

## Logging

Both services use Serilog configured via `appsettings.json`. Structured logging is enabled and request logging middleware is configured in both Program.cs files.

## Client Library Usage

The `Wumpus.Client` project provides a simple client for games to send events and errors:

### Sending Events

```csharp
var options = new WumpusClientOptions
{
    ServerUrl = "http://localhost:5001",
    AppVersion = "1.0.0",
    Platform = "Windows"
};

using var client = new WumpusClient(options);

// Send a game event
await client.SendEventAsync(
    name: "LevelCompleted",
    category: "Gameplay",
    value: 1,
    userId: "player123",
    metadata: new Dictionary<string, object>
    {
        { "level", 5 },
        { "time", 120.5 }
    }
);
```

### Sending Errors

```csharp
// Send a custom error
await client.SendErrorAsync(
    message: "Failed to load texture",
    severity: "Error",
    code: "TEX001",
    exceptionType: "TextureLoadException"
);

// Send a crash report from an exception (convenience method)
try
{
    // Game code
}
catch (Exception ex)
{
    await client.SendCrashAsync(ex);
}
```

### Fire-and-Forget Methods

For scenarios where you don't want to wait for the result:

```csharp
client.SendEventFireAndForget("PlayerJoined", "Multiplayer");
client.SendErrorFireAndForget("Minor issue", severity: "Warning");
client.SendCrashFireAndForget(exception);
```

The client is designed to fail silently (returns null on error) to avoid telemetry from crashing the game.
