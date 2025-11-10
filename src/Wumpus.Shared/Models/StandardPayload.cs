using System.ComponentModel.DataAnnotations;

namespace Wumpus.Shared.Models;

/// <summary>
/// Standard payload containing common identification fields for all reports.
/// </summary>
public class StandardPayload
{
    [Required]
    [MaxLength(50)]
    public string GameVersion { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Platform { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public Guid ComputerId { get; set; }

    public Guid GameId { get; set; }

    public Guid SequenceId { get; set; }
}
