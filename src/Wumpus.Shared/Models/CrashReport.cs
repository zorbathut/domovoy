namespace Wumpus.Shared.Models;

public class CrashReport
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public required CrashReportCore Core { get; set; }
}
