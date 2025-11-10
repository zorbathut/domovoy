using System.ComponentModel.DataAnnotations;

namespace Wumpus.Shared.Models;

public class Error : Report
{
    public Error()
    {
        ReportType = ReportType.Error;
    }

    public required ErrorData Data { get; set; }
}

public class ErrorData
{
    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = "Error"; // Warning, Error, Critical, Fatal

    [MaxLength(100)]
    public string? Code { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ExceptionType { get; set; }

    public string? StackTrace { get; set; }

    public string? Context { get; set; }
}
