using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Domovoy.Shared.Models;

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
    
    public Environment Environment { get; set; }

    public Guid UserId { get; set; }

    public Guid ComputerId { get; set; }

    public Guid GameId { get; set; }

    public List<Guid> GameSequenceIds { get; set; } = new();

    public Guid ProcessId { get; set; }

    // Flexible metadata stored as JSONB
    public Dictionary<string, object>? Metadata { get; set; }
}
