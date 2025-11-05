namespace Wumpus.Shared.Models;

public class CrashReport
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string GameVersion { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string ExceptionMessage { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    public string StackTraceHash { get; set; } = string.Empty;
    public int OccurrenceCount { get; set; } = 1;
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public string? SystemInfo { get; set; }
    public string? UserContext { get; set; }
}
