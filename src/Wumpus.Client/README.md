# Wumpus.Client

A simple C# client library for sending crash reports to a Wumpus crash reporting server.

## Installation

Add a reference to the `Wumpus.Client` project in your application:

```bash
dotnet add reference path/to/Wumpus.Client/Wumpus.Client.csproj
```

## Quick Start

```csharp
using Wumpus.Client;

var options = new WumpusClientOptions
{
    ServerUrl = "https://crashes.example.com",
    AppVersion = "1.0.0",
    Platform = "Windows"
};

using var client = new WumpusClient(options);

try
{
    // Your code
}
catch (Exception ex)
{
    var crashId = await client.SendCrashReportAsync(ex);
    if (crashId.HasValue)
    {
        Console.WriteLine($"Crash report submitted: {crashId.Value}");
    }
}
```

## Configuration Options

```csharp
var options = new WumpusClientOptions
{
    // Required: The base URL of your Wumpus server
    ServerUrl = "https://crashes.example.com",

    // Required: Your application version
    AppVersion = "1.0.0",

    // Required: Platform identifier
    Platform = "Windows",

    // Optional: HTTP request timeout (default: 30 seconds)
    TimeoutSeconds = 30,

    // Optional: Global system info included with every report
    SystemInfo = new Dictionary<string, string>
    {
        { "OS", Environment.OSVersion.ToString() },
        { "Runtime", Environment.Version.ToString() }
    },

    // Optional: Global user context included with every report
    UserContext = new Dictionary<string, string>
    {
        { "UserId", "12345" },
        { "UserName", "john.doe" }
    }
};
```

## Advanced Usage

### Adding Per-Report Context

```csharp
var systemInfo = new Dictionary<string, string>
{
    { "MemoryUsage", GetMemoryUsage() }
};

var userContext = new Dictionary<string, string>
{
    { "CurrentScreen", "MainMenu" }
};

await client.SendCrashReportAsync(exception, systemInfo, userContext);
```

### Fire-and-Forget (Non-Blocking)

Use this when you don't need to wait for the result:

```csharp
try
{
    // Your code
}
catch (Exception ex)
{
    client.SendCrashReportFireAndForget(ex);
}
```

## Example: Console Application

```csharp
using Wumpus.Client;

class Program
{
    static async Task Main(string[] args)
    {
        var options = new WumpusClientOptions
        {
            ServerUrl = "http://localhost:1973",
            AppVersion = "1.0.0",
            Platform = Environment.OSVersion.Platform.ToString(),
            SystemInfo = new Dictionary<string, string>
            {
                { "MachineName", Environment.MachineName },
                { "UserName", Environment.UserName }
            }
        };

        using var client = new WumpusClient(options);

        try
        {
            // Your application code
            DoSomethingRisky();
        }
        catch (Exception ex)
        {
            var crashId = await client.SendCrashReportAsync(ex);
            Console.WriteLine(crashId.HasValue
                ? $"Crash reported: {crashId.Value}"
                : "Failed to report crash");
            throw;
        }
    }
}
```

## Error Handling

The library is designed to fail silently to prevent crash reporting from crashing your application:

- Failed HTTP requests return `null` instead of throwing
- `SendCrashReportFireAndForget` never throws exceptions
- Network errors, timeouts, and server errors are caught internally

## Thread Safety

- `WumpusClient` is thread-safe for sending crash reports
- Multiple reports can be sent concurrently

## License

See the main project LICENSE file.
