# Domovoy - Game Telemetry and Error Tracking

A telemetry and error tracking system for games built with C# and ASP.NET Core 10. It collects and visualizes two types of reports:
- **Events** - Game analytics and custom events (e.g., level completions, user actions)
- **Errors** - Error conditions and crashes with stack traces

## Features

- **Event and Error Intake API** - HTTP endpoints for receiving telemetry from games
- **Web Dashboard** - Blazor Server UI for viewing and analyzing reports
- **Attachment Support** - Upload crash dumps, screenshots, and log files (up to 50 MB) via MinIO
- **Client Library** - Stateless C# client for games with sync, async, and fire-and-forget methods
- **Subscriber/Notification System** - Subscribe to report events and receive notifications
- **Discord Bot** - Optional Discord integration for report notifications
- **PostgreSQL Database** - Reliable storage with JSONB support for flexible metadata
- **Docker Support** - Easy deployment with Docker Compose
- **Health Monitoring** - Built-in health checks for all services
- **Structured Logging** - Serilog integration for comprehensive logging

## Architecture

### Services

- **Intake API** (Port 1973) - Receives events, errors, and attachments
- **Web UI** (Port 1975) - Dashboard for viewing reports, managing subscribers
- **PostgreSQL** (Port 5432) - Database for report data
- **MinIO** (Ports 9000/9001 in dev) - S3-compatible object store for attachments

### Projects

- `Domovoy.Shared` - Shared models (Report, Event, Error, Attachment) and DTOs
- `Domovoy.Database` - EF Core DbContext and migrations
- `Domovoy.Intake` - Intake API service for events, errors, and attachments
- `Domovoy.Web` - Blazor Server web interface with attachment proxy and notification API
- `Domovoy.Client` - Client library for games
- `Domovoy.MinIO` - Shared MinIO/S3 integration for attachment storage
- `Domovoy.NotificationClient` - Client library for the notification system
- `Domovoy.DiscordBot` - Discord bot for report notifications

## Quick Start

### Prerequisites

- .NET 10.0 SDK
- Docker and Docker Compose

### Running with Docker

1. **Start all services:**
   ```bash
   docker-compose up
   ```

2. **Optionally include the Discord bot:**
   ```bash
   docker-compose --profile discord up
   ```

3. **Access the services:**
   - Web UI: http://localhost:1975
   - Intake API: http://localhost:1973
   - Health Checks: http://localhost:1975/health and http://localhost:1973/health

### Running Locally (Development)

1. **Start PostgreSQL and MinIO in Docker:**
   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.dev.yml up postgres minio
   ```

2. **Run the Intake API:**
   ```bash
   dotnet run --project src/Domovoy.Intake/Domovoy.Intake.csproj
   ```

3. **Run the Web UI (in another terminal):**
   ```bash
   dotnet run --project src/Domovoy.Web/Domovoy.Web.csproj
   ```

## Submitting Reports

### Using the Client Library

```csharp
using var client = new DomovoyClient("http://localhost:1973");

var standard = new StandardPayload
{
    GameVersion = "1.0.0",
    Platform = "Windows",
    Environment = Environment.Dev,
    UserId = Guid.NewGuid(),
    ComputerId = Guid.NewGuid(),
    GameId = Guid.NewGuid(),
    GameSequenceIds = [Guid.NewGuid()],
    ProcessId = Guid.NewGuid()
};

// Send an event
Guid? reportId = await client.SendEventAsync(standard, new EventPayload
{
    Name = "LevelCompleted",
    Category = "Gameplay",
    Value = 1,
    Metadata = new Dictionary<string, object> { { "level", 5 } }
});

// Send an error
Guid? errorId = await client.SendErrorAsync(standard, new ErrorPayload
{
    Severity = Severity.Error,
    Message = "Failed to load texture",
    StackTrace = "at Game.TextureLoader.Load() in TextureLoader.cs:line 42",
    Log = "Full error log..."
});

// Upload an attachment against a report
if (reportId != null)
{
    using var stream = File.OpenRead("screenshot.png");
    await client.SendAttachmentAsync(reportId.Value, stream, "screenshot.png", "image/png");
}
```

### API Endpoints

**Submit an event:**
```
POST http://localhost:1973/api/v1/reports/event
Content-Type: application/json
```

```json
{
  "standard": {
    "gameVersion": "1.0.0",
    "platform": "Windows",
    "environment": 0,
    "userId": "00000000-0000-0000-0000-000000000001",
    "computerId": "00000000-0000-0000-0000-000000000002",
    "gameId": "00000000-0000-0000-0000-000000000003",
    "gameSequenceIds": ["00000000-0000-0000-0000-000000000004"],
    "processId": "00000000-0000-0000-0000-000000000005"
  },
  "data": {
    "name": "LevelCompleted",
    "category": "Gameplay",
    "value": 1,
    "userId": "player123",
    "metadata": { "level": 5 }
  }
}
```

**Submit an error:**
```
POST http://localhost:1973/api/v1/reports/error
Content-Type: application/json
```

```json
{
  "standard": {
    "gameVersion": "1.0.0",
    "platform": "Windows",
    "environment": 0,
    "userId": "00000000-0000-0000-0000-000000000001",
    "computerId": "00000000-0000-0000-0000-000000000002",
    "gameId": "00000000-0000-0000-0000-000000000003",
    "gameSequenceIds": ["00000000-0000-0000-0000-000000000004"],
    "processId": "00000000-0000-0000-0000-000000000005"
  },
  "data": {
    "severity": 3,
    "message": "Failed to load texture",
    "stackTrace": "at Game.TextureLoader.Load() in TextureLoader.cs:line 42",
    "log": "Full error log..."
  }
}
```

**Upload an attachment:**
```
POST http://localhost:1973/api/v1/reports/{reportId}/attachments
Content-Type: multipart/form-data
```

**Response** (HTTP 202 Accepted):
```json
{
  "reportId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

## Database Migrations

The database schema is automatically migrated on startup. To create a new migration:

```bash
cd src/Domovoy.Database
dotnet ef migrations add MigrationName
```

To apply migrations manually:

```bash
cd src/Domovoy.Database
dotnet ef database update
```

## Configuration

### Connection String

Update `appsettings.json` in both Intake and Web projects:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=domovoy;Username=domovoy;Password=domovoy"
  }
}
```

### MinIO

MinIO settings are configured in `appsettings.json` under the `Minio` section in both Intake and Web projects.

### Environment Variables (Docker)

See `.env.example` for available environment variables:

- `POSTGRES_DB=domovoy`
- `POSTGRES_USER=domovoy`
- `POSTGRES_PASSWORD=domovoy`
- `ConnectionStrings__DefaultConnection` - Database connection string

## Development

### Building the Solution

```bash
dotnet build
```

### Running Tests

PostgreSQL and MinIO must be running before running tests:

```bash
# Start dependencies
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up postgres minio

# Run all tests
dotnet test
```

Tests use a separate `domovoy_test` database and run sequentially.

### Project Structure

```
Domovoy/
├── src/
│   ├── Domovoy.Shared/              # Shared models and DTOs
│   ├── Domovoy.Database/            # EF Core DbContext and migrations
│   ├── Domovoy.Intake/              # Intake API service
│   ├── Domovoy.Web/                 # Blazor Server web UI
│   ├── Domovoy.Client/              # Client library for games
│   ├── Domovoy.MinIO/               # Shared MinIO/S3 integration
│   ├── Domovoy.NotificationClient/  # Notification system client
│   └── Domovoy.DiscordBot/          # Discord bot integration
├── tests/
│   └── Domovoy.Tests/               # Integration tests
├── docker/
│   ├── Intake.Dockerfile
│   ├── Web.Dockerfile
│   └── DiscordBot.Dockerfile
├── config/
│   └── discord-bot.json
├── docker-compose.yml
├── docker-compose.dev.yml
└── .env.example
```

## Monitoring

### Health Checks

Both services expose health check endpoints:

- Intake API: http://localhost:1973/health
- Web UI: http://localhost:1975/health

### Logs

Logs are output to the console in structured format using Serilog.

To view logs from Docker:

```bash
docker-compose logs -f intake
docker-compose logs -f web
```
