using System.ComponentModel.DataAnnotations;

namespace Wumpus.Shared.Models;

/// <summary>
/// Core crash report data shared across all crash report types.
/// Contains the essential fields that identify and describe a crash.
/// </summary>
public class CrashReportCore
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
}
