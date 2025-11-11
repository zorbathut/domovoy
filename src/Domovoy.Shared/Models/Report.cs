using System;
using System.ComponentModel.DataAnnotations;

namespace Domovoy.Shared.Models;

public abstract class Report
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public ReportType ReportType { get; protected set; }

    [Required]
    public StandardPayload Standard { get; set; } = new();
}
