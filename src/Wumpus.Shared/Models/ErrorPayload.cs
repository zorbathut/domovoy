using System.ComponentModel.DataAnnotations;

namespace Wumpus.Shared.Models;

/// <summary>
/// Payload data for an error report.
/// Used in both DTOs and entities to ensure consistency.
/// </summary>
public class ErrorPayload
{
    [Required]
    public Severity Severity { get; set; } = Severity.Error;

    [Required]
    public required string Message { get; set; }

    [Required]
    public required string StackTrace { get; set; }
    
    [Required]
    public required string Log { get; set; }
}
