using System.ComponentModel.DataAnnotations;

namespace Wumpus.Shared.DTOs;

public class SubmitEventRequest
{
    [Required]
    [MaxLength(50)]
    public string GameVersion { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Platform { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(100)]
    public string Category { get; set; } = "General";

    public decimal? Value { get; set; }

    [MaxLength(100)]
    public string? UserId { get; set; }

    public Dictionary<string, object>? Metadata { get; set; }
}
