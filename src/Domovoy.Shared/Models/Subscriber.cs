using System;

namespace Domovoy.Shared.Models;

public class Subscriber
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public int HeartbeatTimeoutMinutes { get; set; } = 10;
}
