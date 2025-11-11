namespace Wumpus.Shared.DTOs;

public class UpdateSubscriberRequest
{
    public bool? IsActive { get; set; }
    public int? HeartbeatTimeoutMinutes { get; set; }
}
