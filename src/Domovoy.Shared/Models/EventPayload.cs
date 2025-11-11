using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Domovoy.Shared.Models;

/// <summary>
/// Payload data for an event report.
/// Used in both DTOs and entities to ensure consistency.
/// </summary>
public class EventPayload
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(100)]
    public string Category { get; set; } = "General";

    public decimal? Value { get; set; }

    [MaxLength(100)]
    public string? UserId { get; set; }

    // Flexible metadata stored as JSONB
    public Dictionary<string, object>? Metadata { get; set; }
}
