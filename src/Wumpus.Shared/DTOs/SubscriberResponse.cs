namespace Wumpus.Shared.DTOs;

public class SubscriberResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public int HeartbeatTimeoutMinutes { get; set; }
}
