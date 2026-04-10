using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domovoy.Shared.Models;
using Environment = Domovoy.Shared.Models.Environment;

namespace Domovoy.Client.Examples;

/// <summary>
/// Example usage patterns for Domovoy.Client
/// </summary>
public static class Examples
{
    /// <summary>
    /// Example 1: Sending a crash report from an exception
    /// </summary>
    public static async Task BasicCrashExample()
    {
        using var client = new DomovoyClient("http://localhost:1973");

        var standard = new StandardPayload
        {
            GameVersion = "1.0.0",
            Platform = "Windows",
            Environment = Environment.Dev,
            UserId = Guid.CreateVersion7(),
            ComputerId = Guid.CreateVersion7(),
            GameId = Guid.CreateVersion7(),
            GameSequenceIds = [Guid.CreateVersion7()],
            ProcessId = Guid.CreateVersion7()
        };

        try
        {
            ThrowExampleException();
        }
        catch (Exception ex)
        {
            var reportId = await client.SendCrashAsync(standard, ex);
            Console.WriteLine($"Crash reported: {(reportId != null ? "Success" : "Failed")}");
        }
    }

    /// <summary>
    /// Example 2: Sending a custom error
    /// </summary>
    public static async Task SendErrorExample()
    {
        using var client = new DomovoyClient("http://localhost:1973");

        var standard = new StandardPayload
        {
            GameVersion = "1.0.0",
            Platform = "Windows",
            Environment = Environment.Dev,
            UserId = Guid.CreateVersion7(),
            ComputerId = Guid.CreateVersion7(),
            GameId = Guid.CreateVersion7(),
            GameSequenceIds = [Guid.CreateVersion7()],
            ProcessId = Guid.CreateVersion7()
        };

        var errorData = new ErrorPayload
        {
            Severity = Severity.Error,
            Message = "Failed to load texture",
            StackTrace = "at Game.TextureLoader.Load(String path) in TextureLoader.cs:line 42",
            Log = "TextureLoadException: Failed to load texture\n   at Game.TextureLoader.Load(String path) in TextureLoader.cs:line 42\n   at Game.Level.Initialize() in Level.cs:line 15"
        };

        var reportId = await client.SendErrorAsync(standard, errorData);
        Console.WriteLine($"Error reported: {(reportId != null ? "Success" : "Failed")}");
    }

    /// <summary>
    /// Example 3: Sending game events
    /// </summary>
    public static async Task SendEventExample()
    {
        using var client = new DomovoyClient("http://localhost:1973");

        var standard = new StandardPayload
        {
            GameVersion = "1.0.0",
            Platform = "Windows",
            Environment = Environment.Dev,
            UserId = Guid.CreateVersion7(),
            ComputerId = Guid.CreateVersion7(),
            GameId = Guid.CreateVersion7(),
            GameSequenceIds = [Guid.CreateVersion7()],
            ProcessId = Guid.CreateVersion7(),
            Metadata = new Dictionary<string, object>
            {
                { "level", 5 },
                { "timeSeconds", 120.5 },
                { "score", 9500 }
            }
        };

        var eventData = new EventPayload
        {
            Category = "Gameplay",
            Name = "LevelCompleted"
        };

        var reportId = await client.SendEventAsync(standard, eventData);
        Console.WriteLine($"Event reported: {(reportId != null ? "Success" : "Failed")}");
    }

    /// <summary>
    /// Example 4: Fire and forget (non-blocking)
    /// </summary>
    public static void FireAndForgetExample()
    {
        var client = new DomovoyClient("http://localhost:1973");

        var standard = new StandardPayload
        {
            GameVersion = "1.0.0",
            Platform = "macOS",
            Environment = Environment.Release,
            UserId = Guid.CreateVersion7(),
            ComputerId = Guid.CreateVersion7(),
            GameId = Guid.CreateVersion7(),
            GameSequenceIds = [Guid.CreateVersion7()],
            ProcessId = Guid.CreateVersion7()
        };

        try
        {
            ThrowExampleException();
        }
        catch (Exception ex)
        {
            // Report without waiting (useful for shutdown scenarios)
            client.SendCrashFireAndForget(standard, ex);
            Console.WriteLine("Crash report queued for sending");
        }

        // Fire and forget for events
        var eventData = new EventPayload
        {
            Category = "Multiplayer",
            Name = "PlayerJoined"
        };
        client.SendEventFireAndForget(standard, eventData);
    }

    /// <summary>
    /// Example 5: Multiple concurrent reports
    /// </summary>
    public static async Task ConcurrentReportsExample()
    {
        using var client = new DomovoyClient("http://localhost:1973");

        var standard = new StandardPayload
        {
            GameVersion = "1.0.0",
            Platform = "Windows",
            Environment = Environment.Dev,
            UserId = Guid.CreateVersion7(),
            ComputerId = Guid.CreateVersion7(),
            GameId = Guid.CreateVersion7(),
            GameSequenceIds = [Guid.CreateVersion7()],
            ProcessId = Guid.CreateVersion7()
        };

        Exception[] exceptions =
        [
            new InvalidOperationException("Operation 1 failed"),
            new ArgumentException("Argument 2 invalid"),
            new NullReferenceException("Reference 3 null")
        ];

        // Send multiple reports concurrently
        var tasks = exceptions.Select(ex => client.SendCrashAsync(standard, ex));
        var results = await Task.WhenAll(tasks);

        for (int i = 0; i < results.Length; i++)
        {
            Console.WriteLine($"Crash {i + 1}: {(results[i] != null ? "Success" : "Failed")}");
        }
    }

    /// <summary>
    /// Example 6: Varying platforms and versions
    /// </summary>
    public static async Task DiverseDataExample()
    {
        using var client = new DomovoyClient("http://localhost:1973");

        // Send events from different platforms and versions
        var platforms = new[] { "Windows", "Linux", "macOS", "Android", "iOS" };
        var versions = new[] { "1.0.0", "1.1.0", "2.0.0" };

        var random = new Random();

        for (int i = 0; i < 10; i++)
        {
            var standard = new StandardPayload
            {
                GameVersion = versions[random.Next(versions.Length)],
                Platform = platforms[random.Next(platforms.Length)],
                Environment = Environment.Dev,
                UserId = Guid.CreateVersion7(),
                ComputerId = Guid.CreateVersion7(),
                GameId = Guid.CreateVersion7(),
                GameSequenceIds = [Guid.CreateVersion7()],
            ProcessId = Guid.CreateVersion7()
            };

            var eventData = new EventPayload
            {
                Category = "Testing",
                Name = "TestEvent"
            };

            await client.SendEventAsync(standard, eventData);
        }

        Console.WriteLine("Sent 10 events with varied platforms and versions");
    }

    private static void ThrowExampleException()
    {
        throw new InvalidOperationException("This is an example exception for demonstration purposes");
    }
}
