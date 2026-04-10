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
    [MaxLength(100)]
    public required string Category { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    public Dictionary<string, object>? Data { get; set; }
}
