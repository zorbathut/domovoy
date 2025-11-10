using System.ComponentModel.DataAnnotations;

namespace Wumpus.Shared.Models;

public abstract class Report
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public ReportType ReportType { get; protected set; }

    [Required]
    [MaxLength(50)]
    public string GameVersion { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Platform { get; set; } = string.Empty;
}
