using Wumpus.Shared.Models;

namespace Wumpus.Shared.DTOs;

public class CrashReportResponse
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public required CrashReportCore Core { get; set; }
}
