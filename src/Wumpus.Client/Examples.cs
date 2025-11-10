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
            Platform = "Windows",
            UserId = Guid.NewGuid(),
            ComputerId = Guid.NewGuid(),
            GameId = Guid.NewGuid()
        };

        using var client = new WumpusClient(options);

        try
        {
            ThrowExampleException();
        }
        catch (Exception ex)
        {
            var success = await client.SendCrashAsync(ex);
            Console.WriteLine($"Crash reported: {(success ? "Success" : "Failed")}");
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
            Platform = "Windows",
            UserId = Guid.NewGuid(),
            ComputerId = Guid.NewGuid(),
            GameId = Guid.NewGuid()
        };

        using var client = new WumpusClient(options);

        var errorRequest = new SubmitErrorRequest
        {
            Standard = new Wumpus.Shared.Models.StandardPayload(), // Will be set by client
            Data = new Wumpus.Shared.Models.ErrorPayload
            {
                Severity = Wumpus.Shared.Models.Severity.Error,
                Message = "Failed to load texture",
                StackTrace = "at Game.TextureLoader.Load(String path) in TextureLoader.cs:line 42",
                Log = "TextureLoadException: Failed to load texture\n   at Game.TextureLoader.Load(String path) in TextureLoader.cs:line 42\n   at Game.Level.Initialize() in Level.cs:line 15"
            }
        };

        var success = await client.SendErrorAsync(errorRequest);
        Console.WriteLine($"Error reported: {(success ? "Success" : "Failed")}");
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
            Platform = "Windows",
            UserId = Guid.NewGuid(),
            ComputerId = Guid.NewGuid(),
            GameId = Guid.NewGuid()
        };

        using var client = new WumpusClient(options);

        var eventRequest = new SubmitEventRequest
        {
            Standard = new Wumpus.Shared.Models.StandardPayload(), // Will be set by client
            Data = new Wumpus.Shared.Models.EventPayload
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
            }
        };

        var success = await client.SendEventAsync(eventRequest);
        Console.WriteLine($"Event reported: {(success ? "Success" : "Failed")}");
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
            Platform = "macOS",
            UserId = Guid.NewGuid(),
            ComputerId = Guid.NewGuid(),
            GameId = Guid.NewGuid()
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
            Standard = new Wumpus.Shared.Models.StandardPayload(), // Will be set by client
            Data = new Wumpus.Shared.Models.EventPayload
            {
                Name = "PlayerJoined",
                Category = "Multiplayer"
            }
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
            Platform = "Windows",
            UserId = Guid.NewGuid(),
            ComputerId = Guid.NewGuid(),
            GameId = Guid.NewGuid()
        });

        Exception[] exceptions =
        [
            new InvalidOperationException("Operation 1 failed"),
            new ArgumentException("Argument 2 invalid"),
            new NullReferenceException("Reference 3 null")
        ];

        // Send multiple reports concurrently
        var tasks = exceptions.Select(ex => client.SendCrashAsync(ex));
        var results = await Task.WhenAll(tasks);

        for (int i = 0; i < results.Length; i++)
        {
            Console.WriteLine($"Crash {i + 1}: {(results[i] ? "Success" : "Failed")}");
        }
    }

    private static void ThrowExampleException()
    {
        throw new InvalidOperationException("This is an example exception for demonstration purposes");
    }
}
