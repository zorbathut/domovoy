namespace Wumpus.Shared.DTOs;

public class EventResponse
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public required string GameVersion { get; set; }
    public required string Platform { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public decimal? Value { get; set; }
    public string? UserId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
