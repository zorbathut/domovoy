using Wumpus.Shared.DTOs;

namespace Wumpus.Client.Examples;

/// <summary>
/// Example usage patterns for Wumpus.Client
/// </summary>
public static class Examples
{
    /// <summary>
    /// Example 1: Sending a crash report from an exception
    /// </summary>
    public static async Task BasicCrashExample()
    {
        var options = new WumpusClientOptions
        {
            ServerUrl = "http://localhost:5000",
            AppVersion = "1.0.0",
            Platform = "Windows"
        };

        using var client = new WumpusClient(options);

        try
        {
            ThrowExampleException();
        }
        catch (Exception ex)
        {
            var crashId = await client.SendCrashAsync(ex);
            Console.WriteLine($"Crash reported with ID: {crashId}");
        }
    }

    /// <summary>
    /// Example 2: Sending a custom error
    /// </summary>
    public static async Task SendErrorExample()
    {
        var options = new WumpusClientOptions
        {
            ServerUrl = "http://localhost:5000",
            AppVersion = "1.0.0",
            Platform = "Windows"
        };

        using var client = new WumpusClient(options);

        var errorRequest = new SubmitErrorRequest
        {
            Severity = "Error",
            Code = "TEX001",
            Message = "Failed to load texture",
            ExceptionType = "TextureLoadException",
            Context = "Level 5 initialization"
        };

        var errorId = await client.SendErrorAsync(errorRequest);
        Console.WriteLine($"Error reported with ID: {errorId}");
    }

    /// <summary>
    /// Example 3: Sending game events
    /// </summary>
    public static async Task SendEventExample()
    {
        var options = new WumpusClientOptions
        {
            ServerUrl = "http://localhost:5000",
            AppVersion = "1.0.0",
            Platform = "Windows"
        };

        using var client = new WumpusClient(options);

        var eventRequest = new SubmitEventRequest
        {
            Name = "LevelCompleted",
            Category = "Gameplay",
            Value = 1,
            UserId = "player123",
            Metadata = new Dictionary<string, object>
            {
                { "level", 5 },
                { "timeSeconds", 120.5 },
                { "score", 9500 }
            }
        };

        var eventId = await client.SendEventAsync(eventRequest);
        Console.WriteLine($"Event reported with ID: {eventId}");
    }

    /// <summary>
    /// Example 4: Fire and forget (non-blocking)
    /// </summary>
    public static void FireAndForgetExample()
    {
        var client = new WumpusClient(new WumpusClientOptions
        {
            ServerUrl = "http://localhost:5000",
            AppVersion = "1.0.0",
            Platform = "macOS"
        });

        try
        {
            ThrowExampleException();
        }
        catch (Exception ex)
        {
            // Report without waiting (useful for shutdown scenarios)
            client.SendCrashFireAndForget(ex);
            Console.WriteLine("Crash report queued for sending");
        }

        // Fire and forget for events
        var eventRequest = new SubmitEventRequest
        {
            Name = "PlayerJoined",
            Category = "Multiplayer"
        };
        client.SendEventFireAndForget(eventRequest);
    }

    /// <summary>
    /// Example 5: Multiple concurrent reports
    /// </summary>
    public static async Task ConcurrentReportsExample()
    {
        using var client = new WumpusClient(new WumpusClientOptions
        {
            ServerUrl = "http://localhost:5000",
            AppVersion = "1.0.0",
            Platform = "Windows"
        });

        Exception[] exceptions =
        [
            new InvalidOperationException("Operation 1 failed"),
            new ArgumentException("Argument 2 invalid"),
            new NullReferenceException("Reference 3 null")
        ];

        // Send multiple reports concurrently
        var tasks = exceptions.Select(ex => client.SendCrashAsync(ex));
        var crashIds = await Task.WhenAll(tasks);

        foreach (var crashId in crashIds.Where(id => id.HasValue))
        {
            Console.WriteLine($"Crash reported: {crashId}");
        }
    }

    private static void ThrowExampleException()
    {
        throw new InvalidOperationException("This is an example exception for demonstration purposes");
    }
}
