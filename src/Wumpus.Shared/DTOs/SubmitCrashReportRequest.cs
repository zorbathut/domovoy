using System.ComponentModel.DataAnnotations;

namespace Wumpus.Shared.DTOs;

public class SubmitCrashReportRequest
{
    [Required]
    [MaxLength(50)]
    public string GameVersion { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Platform { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ExceptionType { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string ExceptionMessage { get; set; } = string.Empty;

    [Required]
    public string StackTrace { get; set; } = string.Empty;

    public Dictionary<string, string>? SystemInfo { get; set; }
    public Dictionary<string, string>? UserContext { get; set; }
}
