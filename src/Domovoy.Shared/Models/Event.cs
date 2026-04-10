using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Domovoy.Shared.Models;

public class Event : Report
{
    public Event()
    {
        ReportType = ReportType.Event;
    }

    [Required]
    [MaxLength(100)]
    public required string Category { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    public Dictionary<string, object>? Data { get; set; }
}
