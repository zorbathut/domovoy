# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Domovoy is a telemetry and error tracking system for games built with C# and ASP.NET Core 9. It collects and visualizes two types of reports:
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

- **Domovoy.Shared** - Shared models (`Report` base class, `Event` and `Error` types) and DTOs used across all projects
- **Domovoy.Database** - EF Core DbContext, migrations, and database configuration
- **Domovoy.Intake** - ASP.NET Core Web API that receives events and errors via HTTP
- **Domovoy.Web** - Blazor Server web interface for viewing events and errors
- **Domovoy.Client** - Client library for games to send events and errors to the Intake API

## Common Development Commands

### Building
```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/Domovoy.Intake/Domovoy.Intake.csproj
```

### Running Services

**Local development (services outside Docker):**
```bash
# Start PostgreSQL only
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up postgres

# Run Intake API (in separate terminal)
dotnet run --project src/Domovoy.Intake/Domovoy.Intake.csproj

# Run Web UI (in separate terminal)
dotnet run --project src/Domovoy.Web/Domovoy.Web.csproj
```

**Full Docker deployment:**
```bash
docker-compose up
```

### Database Migrations

Migrations are automatically applied on startup by both Intake and Web services (see Program.cs in each project).

To create a new migration:
```bash
# Must be run from the Domovoy.Database directory
cd src/Domovoy.Database
dotnet ef migrations add MigrationName
```

To manually apply migrations:
```bash
cd src/Domovoy.Database
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

The solution includes a single test project `Domovoy.Tests` that contains integration tests for all components:

- **IntakeApiTests** - Tests the Intake API HTTP endpoints and database persistence
- **ReportServiceTests** - Tests the ReportService directly, including event and error creation
- **DomovoyClientTests** - Tests the client library's end-to-end integration with the Intake API
- **WebUiTests** - Tests Blazor components using bUnit, verifying UI rendering and data display

#### Test Database

Tests use a separate `domovoy_test` database to avoid interfering with development data:

- The `DatabaseFixture` (in `tests/Domovoy.Tests/Infrastructure/DatabaseFixture.cs`) manages the test database lifecycle
- The test database is dropped and recreated before each test run to ensure clean state
- Tests clean up after themselves using `IAsyncLifetime.DisposeAsync()`
- Connection string: `Host=localhost;Database=domovoy_test;Username=domovoy;Password=domovoy`

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
3. The first test run will create the `domovoy_test` database automatically
4. Each test class cleans up after itself, so tests can be run repeatedly

**Note**: Tests are configured to run sequentially (not in parallel) via `xunit.runner.json` because they share a single test database. This ensures reliability but makes tests slightly slower (~2-4 seconds total).

## Architecture Details

### Service Communication

- **Intake API** (port 1973) and **Web UI** (port 1975) do NOT communicate with each other
- Both services independently connect to the same PostgreSQL database
- Events and errors submitted to the Intake API are stored in the database and retrieved by the Web UI

### Data Model Design

The system uses a Table-Per-Type (TPT) pattern with a `Report` base class and two concrete types. This provides normalized storage with separate tables for each type while maintaining a shared base table for common fields:

**Database Tables:**

1. **Reports** (base table)
   - `Id` (Guid, PK)
   - `Timestamp` (DateTime, indexed)
   - `ReportType` (enum: Event=1, Error=2)
   - `GameVersion` (string, max 50 chars, indexed)
   - `Platform` (string, max 50 chars, indexed)

2. **Events** (inherits from Reports via FK)
   - `Id` (Guid, PK/FK to Reports.Id)
   - `Data_Name` (string, max 200 chars) - Event name
   - `Data_Category` (string, max 100 chars) - Event category
   - `Data_Value` (decimal?) - Optional numeric value
   - `Data_UserId` (string?, max 100 chars, indexed) - Optional user identifier
   - `Data_Metadata` (jsonb) - Optional JSONB metadata

3. **Errors** (inherits from Reports via FK)
   - `Id` (Guid, PK/FK to Reports.Id)
   - `Data_Severity` (string, max 20 chars, indexed) - Error severity level
   - `Data_Code` (string?, max 100 chars) - Optional error code
   - `Data_Message` (string, max 2000 chars) - Error message
   - `Data_ExceptionType` (string?, max 500 chars) - Exception type
   - `Data_StackTrace` (text) - Full stack trace
   - `Data_Context` (text) - Additional context

**TPT Benefits:**
- ✅ No NULL columns - each table only contains relevant fields
- ✅ Can enforce NOT NULL at database level for type-specific fields
- ✅ Clearer schema - separate tables for logically distinct types
- ✅ Efficient indexes - no wasted index space on irrelevant rows
- ✅ Extensible - easy to add new report types in the future

**Shared Payload Types**: `EventPayload` and `ErrorPayload` are used in both DTOs (SubmitEventRequest, SubmitErrorRequest) and entities (Event, Error) to eliminate duplication and ensure consistency. EF Core automatically handles JOINs between Reports and Events/Errors tables when querying. See `DomovoyDbContext.cs` for the EF Core TPT configuration.

**Important**: Each submission creates a new record - there is NO deduplication. Every event and error is stored individually.

### Connection String Configuration

- **Development (local)**: `Host=localhost;Database=domovoy;Username=domovoy;Password=domovoy`
- **Docker**: `Host=postgres;Database=domovoy;Username=domovoy;Password=domovoy`
- Configured in `appsettings.json` (or `appsettings.Development.json`) in both Intake and Web projects
- Docker services use environment variable `ConnectionStrings__DefaultConnection` (see docker-compose.yml)

### Database Design Time Factory

`DomovoyDbContextFactory.cs` provides a design-time factory for EF Core migrations. It uses a hardcoded localhost connection string that is only used during `dotnet ef migrations add` commands, not at runtime.

## Service Ports

- **Web UI**: 1975
- **Intake API**: 1973
- **PostgreSQL**: 5432

Both Intake and Web expose `/health` endpoints for health checks.

## Logging

Both services use Serilog configured via `appsettings.json`. Structured logging is enabled and request logging middleware is configured in both Program.cs files.

## Client Library Usage

The `Domovoy.Client` project provides a stateless client for games to send events and errors.

### Basic Setup

```csharp
using var client = new DomovoyClient("http://localhost:1973");

// Create a standard payload that will be sent with each report
var standard = new StandardPayload
{
    GameVersion = "1.0.0",
    Platform = "Windows",
    Environment = Environment.Dev,
    UserId = Guid.NewGuid(),
    ComputerId = Guid.NewGuid(),
    GameId = Guid.NewGuid(),
    SequenceId = Guid.NewGuid()  // Generate unique ID per request
};
```

### Sending Events

```csharp
var eventData = new EventPayload
{
    Name = "LevelCompleted",
    Category = "Gameplay",
    Value = 1,
    UserId = "player123",
    Metadata = new Dictionary<string, object>
    {
        { "level", 5 },
        { "timeSeconds", 120.5 }
    }
};

await client.SendEventAsync(standard, eventData);
```

### Sending Errors

```csharp
// Send a custom error
var errorData = new ErrorPayload
{
    Severity = Severity.Error,
    Message = "Failed to load texture",
    StackTrace = "at Game.TextureLoader.Load() in TextureLoader.cs:line 42",
    Log = "Full error log..."
};

await client.SendErrorAsync(standard, errorData);

// Send a crash report from an exception (convenience method)
try
{
    // Game code
}
catch (Exception ex)
{
    await client.SendCrashAsync(standard, ex);  // Builds ErrorPayload automatically
}
```

### Fire-and-Forget Methods

For scenarios where you don't want to wait for the result:

```csharp
client.SendEventFireAndForget(standard, eventData);
client.SendErrorFireAndForget(standard, errorData);
client.SendCrashFireAndForget(standard, exception);
```

**Note**: The client is stateless - you provide the StandardPayload with each request, allowing you to easily vary platform, version, and other metadata per report. The client is designed to fail silently (returns false on error) to avoid telemetry from crashing the game.
