namespace Wumpus.Shared.DTOs;

public class CrashReportResponse
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string GameVersion { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string ExceptionMessage { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    public int OccurrenceCount { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public Dictionary<string, string>? SystemInfo { get; set; }
    public Dictionary<string, string>? UserContext { get; set; }
}
