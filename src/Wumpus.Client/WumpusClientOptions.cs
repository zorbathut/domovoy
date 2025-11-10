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
    /// The application version to include in reports.
    /// </summary>
    public required string AppVersion { get; set; }

    /// <summary>
    /// The platform identifier (e.g., "Windows", "Linux", "macOS", "Android", "iOS").
    /// </summary>
    public required string Platform { get; set; }

    /// <summary>
    /// The build environment (Dev or Release).
    /// </summary>
    public required Shared.Models.Environment Environment { get; set; }

    /// <summary>
    /// Unique identifier for the user.
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    /// Unique identifier for the computer/device.
    /// </summary>
    public required Guid ComputerId { get; set; }

    /// <summary>
    /// Unique identifier for the game instance.
    /// </summary>
    public required Guid GameId { get; set; }

    /// <summary>
    /// Timeout for HTTP requests in seconds. Default is 30 seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
