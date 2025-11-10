namespace Wumpus.Client;

/// <summary>
/// Configuration options for the Wumpus crash reporting client.
/// </summary>
public class WumpusClientOptions
{
    /// <summary>
    /// The base URL of the Wumpus crash reporting server (e.g., "https://crashes.example.com").
    /// </summary>
    public required string ServerUrl { get; set; }

    /// <summary>
    /// The application version to include in crash reports.
    /// </summary>
    public required string AppVersion { get; set; }

    /// <summary>
    /// The platform identifier (e.g., "Windows", "Linux", "macOS", "Android", "iOS").
    /// </summary>
    public required string Platform { get; set; }

    /// <summary>
    /// Timeout for HTTP requests in seconds. Default is 30 seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
