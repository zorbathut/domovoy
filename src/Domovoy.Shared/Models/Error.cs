using System.ComponentModel.DataAnnotations;

namespace Domovoy.Shared.Models;

public class Error : Report
{
    public Error()
    {
        ReportType = ReportType.Error;
    }

    [Required]
    public Severity Severity { get; set; } = Severity.Error;

    [Required]
    public required string Message { get; set; }

    [Required]
    public required string StackTrace { get; set; }

    [Required]
    public required string Log { get; set; }
}
