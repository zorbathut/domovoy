# Wumpus - Game Crash Tracker

A crash tracker and analytics system for games built with C# and ASP.NET Core 9.

## Features

- **Crash Intake API** - HTTP endpoint for receiving crash reports from games
- **Web Dashboard** - Blazor Server UI for viewing and analyzing crash reports
- **Automatic Deduplication** - Groups similar crashes based on stack trace hashing
- **PostgreSQL Database** - Reliable storage with JSONB support for flexible metadata
- **Docker Support** - Easy deployment with Docker Compose
- **Health Monitoring** - Built-in health checks for all services
- **Structured Logging** - Serilog integration for comprehensive logging

## Architecture

### Services

- **Intake API** (Port 5001) - Receives and processes crash reports
- **Web UI** (Port 5000) - Dashboard for viewing crashes
- **PostgreSQL** (Port 5432) - Database for crash data

### Projects

- `Wumpus.Shared` - Shared models and DTOs
- `Wumpus.Database` - EF Core DbContext and migrations
- `Wumpus.Intake` - Crash intake API service
- `Wumpus.Web` - Blazor Server web interface

## Quick Start

### Prerequisites

- .NET 9.0 SDK
- Docker and Docker Compose
- (Optional) PostgreSQL 16+ if running locally

### Running with Docker

1. **Start all services:**
   ```bash
   docker-compose up
   ```

2. **Access the services:**
   - Web UI: http://localhost:5000
   - Intake API: http://localhost:5001
   - Health Checks: http://localhost:5000/health and http://localhost:5001/health

### Running Locally (Development)

1. **Start only PostgreSQL in Docker:**
   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.dev.yml up postgres
   ```

2. **Run the Intake API:**
   ```bash
   dotnet run --project src/Wumpus.Intake/Wumpus.Intake.csproj
   ```

3. **Run the Web UI (in another terminal):**
   ```bash
   dotnet run --project src/Wumpus.Web/Wumpus.Web.csproj
   ```

## Submitting Crash Reports

### API Endpoint

```
POST http://localhost:5001/api/v1/crashes
Content-Type: application/json
```

### Example Request

```json
{
  "gameVersion": "1.0.0",
  "platform": "Windows",
  "exceptionType": "System.NullReferenceException",
  "exceptionMessage": "Object reference not set to an instance of an object.",
  "stackTrace": "   at MyGame.Player.Update() in C:\\Game\\Player.cs:line 42\\n   at MyGame.GameLoop.Tick() in C:\\Game\\GameLoop.cs:line 123",
  "systemInfo": {
    "OS": "Windows 11",
    "RAM": "16GB",
    "GPU": "NVIDIA RTX 3080"
  },
  "userContext": {
    "userId": "user123",
    "level": "5",
    "sessionId": "abc-def-123"
  }
}
```

### Example with curl

```bash
curl -X POST http://localhost:5001/api/v1/crashes \\
  -H "Content-Type: application/json" \\
  -d '{
    "gameVersion": "1.0.0",
    "platform": "Windows",
    "exceptionType": "System.NullReferenceException",
    "exceptionMessage": "Object reference not set to an instance of an object.",
    "stackTrace": "   at MyGame.Player.Update()\n   at MyGame.GameLoop.Tick()",
    "systemInfo": {
      "OS": "Windows 11"
    }
  }'
```

### Response

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

HTTP Status: `202 Accepted`

## Database Migrations

The database schema is automatically migrated on startup. To create a new migration:

```bash
cd src/Wumpus.Database
dotnet ef migrations add MigrationName
```

To apply migrations manually:

```bash
cd src/Wumpus.Database
dotnet ef database update
```

## Configuration

### Connection String

Update `appsettings.json` in both Intake and Web projects:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=wumpus;Username=wumpus;Password=wumpus"
  }
}
```

### Environment Variables (Docker)

The `docker-compose.yml` file uses environment variables:

- `POSTGRES_DB=wumpus`
- `POSTGRES_USER=wumpus`
- `POSTGRES_PASSWORD=wumpus`
- `ConnectionStrings__DefaultConnection` - Database connection string

## Development

### Building the Solution

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Project Structure

```
Wumpus/
├── src/
│   ├── Wumpus.Shared/        # Shared models and DTOs
│   ├── Wumpus.Database/       # EF Core DbContext and migrations
│   ├── Wumpus.Intake/         # Crash intake API
│   └── Wumpus.Web/            # Blazor Server web UI
├── docker/
│   ├── Intake.Dockerfile      # Dockerfile for Intake API
│   └── Web.Dockerfile         # Dockerfile for Web UI
├── docker-compose.yml         # Main compose file
├── docker-compose.dev.yml     # Development overrides
└── README.md
```

## How It Works

### Crash Deduplication

Wumpus automatically groups similar crashes together:

1. When a crash report is received, the first 5 stack frames are extracted
2. A SHA-256 hash is computed from these frames
3. If a crash with the same hash exists, the occurrence count is incremented
4. Otherwise, a new crash record is created

### Data Model

**CrashReport**:
- `Id` - Unique identifier
- `Timestamp` - When the crash was first reported
- `GameVersion` - Version of the game
- `Platform` - Windows, Linux, Mac, etc.
- `ExceptionType` - Type of exception (e.g., NullReferenceException)
- `ExceptionMessage` - Error message
- `StackTrace` - Full stack trace
- `StackTraceHash` - Hash for deduplication
- `OccurrenceCount` - Number of times this crash has occurred
- `FirstSeen` / `LastSeen` - Timestamps
- `SystemInfo` - Optional JSON metadata about the system
- `UserContext` - Optional JSON metadata about the user/session

## Monitoring

### Health Checks

Both services expose health check endpoints:

- Intake API: http://localhost:5001/health
- Web UI: http://localhost:5000/health

Health checks verify:
- Service is running
- Database connection is healthy

### Logs

Logs are output to the console in structured format using Serilog.

To view logs from Docker:

```bash
docker-compose logs -f intake
docker-compose logs -f web
```

## Troubleshooting

### Database Connection Issues

If services can't connect to PostgreSQL:

1. Check that PostgreSQL is running:
   ```bash
   docker-compose ps postgres
   ```

2. Verify the connection string in `appsettings.json`

3. Check PostgreSQL logs:
   ```bash
   docker-compose logs postgres
   ```

### Port Conflicts

If ports 5000, 5001, or 5432 are already in use, modify `docker-compose.yml`:

```yaml
ports:
  - "5010:5000"  # Map external port 5010 to internal port 5000 (change left side only)
```

## Future Enhancements

- API key authentication for intake endpoint
- Web UI authentication
- Advanced filtering and search
- Crash trend analysis and charts
- Email notifications for new crash types
- Crash report retention policies
- Rate limiting on intake API
- Symbol/source map support for better stack traces
