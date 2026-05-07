using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Domovoy.Shared.DTOs;

public class SubmitReportRequest
{
    [Required]
    [MaxLength(50)]
    public required string Version { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Platform { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Environment { get; set; }

    public Guid UserId { get; set; }

    public Guid ComputerId { get; set; }

    public Guid CampaignId { get; set; }

    public List<Guid> CampaignSequenceIds { get; set; } = new();

    public Guid ProcessId { get; set; }

    public Dictionary<string, object>? Metadata { get; set; }

    public DateTime? GeneratedAt { get; set; }
}
