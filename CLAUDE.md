# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Wumpus is a crash tracker and analytics system for games built with C# and ASP.NET Core 9. It consists of multiple services that work together to collect, deduplicate, and visualize game crash reports.

## Development Philosophy

This project follows MVP/KISS principles (Minimum Viable Product / Keep It Simple, Stupid). When making changes or adding features:
- Include what's necessary, not more
- Avoid over-engineering or adding speculative features
- Keep implementations straightforward and maintainable

## Solution Structure

The solution contains 5 projects:

- **Wumpus.Shared** - Shared models, DTOs, and the `CrashReportCore` value object used across all projects
- **Wumpus.Database** - EF Core DbContext, migrations, and database configuration
- **Wumpus.Intake** - ASP.NET Core Web API that receives crash reports via HTTP
- **Wumpus.Web** - Blazor Server web interface for viewing crash reports
- **Wumpus.Client** - Client library for games to send crash reports to the Intake API

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

- **IntakeApiTests** - Tests the Intake API HTTP endpoints, database persistence, and deduplication logic
- **CrashReportServiceTests** - Tests the CrashReportService directly, including stack trace hashing and deduplication
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
- **TestDataBuilder** - Fluent builder for creating test crash report data

#### What the Tests Cover

1. **API Integration** - Full HTTP request/response cycle including serialization, validation, and error handling
2. **Database Operations** - Actual PostgreSQL queries, migrations, and data persistence
3. **Deduplication Logic** - Stack trace hashing (first 5 frames) and crash grouping behavior
4. **Client Library** - End-to-end crash report submission from client to database
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
- Crash reports submitted to the Intake API are stored in the database and retrieved by the Web UI

### Crash Deduplication Strategy

The deduplication logic is in `CrashReportService.cs:71-79`:
1. Extract the first 5 stack frames from the crash stack trace
2. Compute SHA-256 hash of those frames
3. Check if a `CrashReport` with that `StackTraceHash` exists
4. If exists: increment `OccurrenceCount` and update `LastSeen`
5. If new: create new `CrashReport` with `OccurrenceCount=1`

This means crashes are considered "the same" if their top 5 stack frames match, regardless of platform, game version, or other metadata.

### Data Model Design

The `CrashReport` entity uses an owned entity pattern for core crash data:

- `CrashReport` (table: CrashReports)
  - `Id` (Guid, PK)
  - `Timestamp`, `StackTraceHash`, `OccurrenceCount`, `FirstSeen`, `LastSeen`
  - `Core` (owned entity of type `CrashReportCore`) - contains GameVersion, Platform, ExceptionType, ExceptionMessage, StackTrace
  - `SystemInfo`, `UserContext` (JSONB fields for flexible metadata)

The owned entity pattern means `CrashReportCore` fields are stored as columns in the CrashReports table, not a separate table. See `WumpusDbContext.cs:40-64` for the EF Core configuration.

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

The `Wumpus.Client` project provides a simple client for games to send crash reports:

```csharp
var options = new WumpusClientOptions
{
    ServerUrl = "http://localhost:5001",
    AppVersion = "1.0.0",
    Platform = "Windows"
};

using var client = new WumpusClient(options);
await client.SendCrashReportAsync(exception);
```

The client is designed to fail silently (returns null on error) to avoid crash reporting from crashing the game.
