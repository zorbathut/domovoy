using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Domovoy.Shared.Models;

public abstract class Report
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public ReportType ReportType { get; protected set; }

    [Required]
    [MaxLength(50)]
    public string Version { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Platform { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Environment { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public Guid ComputerId { get; set; }

    public Guid CampaignId { get; set; }

    public List<Guid> CampaignSequenceIds { get; set; } = new();

    public Guid ProcessId { get; set; }

    public Dictionary<string, object>? Metadata { get; set; }
}
