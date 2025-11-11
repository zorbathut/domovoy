namespace Wumpus.Shared.DTOs;

public class RegisterSubscriberRequest
{
    public string Name { get; set; } = string.Empty;
    public int? HeartbeatTimeoutMinutes { get; set; }
}
