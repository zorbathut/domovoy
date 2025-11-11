namespace Wumpus.Shared.Models;

public class Notification
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public Guid SubscriberId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LockedUntil { get; set; }
    public string? LockedBy { get; set; }
    public int RetryCount { get; set; } = 0;
    public DateTime? LastAttemptAt { get; set; }

    // Navigation properties
    public Report Report { get; set; } = null!;
    public Subscriber Subscriber { get; set; } = null!;
}
