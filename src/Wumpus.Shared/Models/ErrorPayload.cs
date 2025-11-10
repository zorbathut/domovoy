using System.ComponentModel.DataAnnotations;

namespace Wumpus.Shared.Models;

/// <summary>
/// Payload data for an error report.
/// Used in both DTOs and entities to ensure consistency.
/// </summary>
public class ErrorPayload
{
    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = "Error";

    [MaxLength(100)]
    public string? Code { get; set; }

    [Required]
    [MaxLength(2000)]
    public required string Message { get; set; }

    [MaxLength(500)]
    public string? ExceptionType { get; set; }

    public string? StackTrace { get; set; }

    public string? Context { get; set; }
}
