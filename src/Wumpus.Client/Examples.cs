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
            var crashId = await client.SendCrashReportAsync(ex);
            Console.WriteLine($"Crash reported with ID: {crashId}");
        }
    }

    /// <summary>
    /// Example 2: Using the client with custom context
    /// </summary>
    public static async Task ClientWithContextExample()
    {
        var options = new WumpusClientOptions
        {
            ServerUrl = "http://localhost:5000",
            AppVersion = "2.0.0",
            Platform = "Linux",
            SystemInfo = new Dictionary<string, string>
            {
                { "OS", Environment.OSVersion.ToString() },
                { "Runtime", Environment.Version.ToString() },
                { "MachineName", Environment.MachineName }
            },
            UserContext = new Dictionary<string, string>
            {
                { "UserId", "user-12345" }
            }
        };

        using var client = new WumpusClient(options);

        try
        {
            ThrowExampleException();
        }
        catch (Exception ex)
        {
            // Add per-report context
            var additionalSystemInfo = new Dictionary<string, string>
            {
                { "MemoryUsageMB", GC.GetTotalMemory(false) / 1024 / 1024 + "MB" }
            };

            var additionalUserContext = new Dictionary<string, string>
            {
                { "CurrentScreen", "MainMenu" },
                { "ActionPerformed", "LoadGame" }
            };

            var crashId = await client.SendCrashReportAsync(
                ex,
                additionalSystemInfo,
                additionalUserContext
            );

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
            client.SendCrashReportFireAndForget(ex);
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
        var tasks = exceptions.Select(ex => client.SendCrashReportAsync(ex));
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
