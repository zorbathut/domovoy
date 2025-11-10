using System.ComponentModel.DataAnnotations;

namespace Wumpus.Shared.DTOs;

public class SubmitErrorRequest
{
    [Required]
    [MaxLength(50)]
    public string GameVersion { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Platform { get; set; } = null!;

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
