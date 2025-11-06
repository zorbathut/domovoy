namespace Wumpus.Shared.Models;

public class CrashReport
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public required CrashReportCore Core { get; set; }
    public string StackTraceHash { get; set; } = string.Empty;
    public int OccurrenceCount { get; set; } = 1;
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public string? SystemInfo { get; set; }
    public string? UserContext { get; set; }
}
