using Wumpus.Shared.Models;

namespace Wumpus.Shared.DTOs;

public class NotificationResponse
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int RetryCount { get; set; }
    public ReportDto Report { get; set; } = null!;
}

public class ReportDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string ReportType { get; set; } = string.Empty; // "Event" or "Error"
    public StandardPayload Standard { get; set; } = null!;
    public EventPayload? EventData { get; set; }
    public ErrorPayload? ErrorData { get; set; }
}
