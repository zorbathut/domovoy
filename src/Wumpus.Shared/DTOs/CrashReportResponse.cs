using Wumpus.Shared.Models;

namespace Wumpus.Shared.DTOs;

public class CrashReportResponse
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public required CrashReportCore Core { get; set; }
    public int OccurrenceCount { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public Dictionary<string, string>? SystemInfo { get; set; }
    public Dictionary<string, string>? UserContext { get; set; }
}
