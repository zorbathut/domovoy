namespace Wumpus.Shared.DTOs;

public class ErrorResponse
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public required string GameVersion { get; set; }
    public required string Platform { get; set; }
    public required string Severity { get; set; }
    public string? Code { get; set; }
    public required string Message { get; set; }
    public string? ExceptionType { get; set; }
    public string? StackTrace { get; set; }
    public string? Context { get; set; }
}
