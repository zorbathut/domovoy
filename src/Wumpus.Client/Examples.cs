namespace Wumpus.Client.Examples;

/// <summary>
/// Example usage patterns for Wumpus.Client
/// </summary>
public static class Examples
{
    /// <summary>
    /// Example 1: Basic usage
    /// </summary>
    public static async Task BasicUsageExample()
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
    /// Example 2: Using the client with different configuration
    /// </summary>
    public static async Task ClientWithCustomConfigExample()
    {
        var options = new WumpusClientOptions
        {
            ServerUrl = "http://localhost:5000",
            AppVersion = "2.0.0",
            Platform = "Linux",
            TimeoutSeconds = 60 // Custom timeout
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
    /// Example 3: Fire and forget (non-blocking)
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
    }

    /// <summary>
    /// Example 4: Multiple concurrent reports
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
