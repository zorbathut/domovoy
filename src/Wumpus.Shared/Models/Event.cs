using System.ComponentModel.DataAnnotations;

namespace Wumpus.Shared.Models;

public class Event : Report
{
    public Event()
    {
        ReportType = ReportType.Event;
    }

    public required EventData Data { get; set; }
}

public class EventData
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Category { get; set; } = "General";

    public decimal? Value { get; set; }

    [MaxLength(100)]
    public string? UserId { get; set; }

    // Flexible metadata stored as JSONB
    public Dictionary<string, object>? Metadata { get; set; }
}
