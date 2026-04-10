using System;
using System.Collections.Generic;
using Domovoy.Shared.Models;

namespace Domovoy.Shared.DTOs;

public class NotificationResponse
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int RetryCount { get; set; }
    public ReportDto Report { get; set; } = null!;
}

public class ReportDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string ReportType { get; set; } = string.Empty;

    // Common fields
    public string Version { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public Models.Environment Environment { get; set; }
    public Guid UserId { get; set; }
    public Guid ComputerId { get; set; }
    public Guid CampaignId { get; set; }
    public List<Guid> CampaignSequenceIds { get; set; } = new();
    public Guid ProcessId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    // Event fields (null if error)
    public string? Category { get; set; }
    public string? Name { get; set; }
    public Dictionary<string, object>? Data { get; set; }

    // Error fields (null if event)
    public Severity? Severity { get; set; }
    public string? Message { get; set; }
    public string? StackTrace { get; set; }
    public string? Log { get; set; }
}
