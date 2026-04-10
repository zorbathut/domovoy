using System.ComponentModel.DataAnnotations;
using Domovoy.Shared.Models;

namespace Domovoy.Shared.DTOs;

public class SubmitErrorRequest : SubmitReportRequest
{
    public Severity Severity { get; set; } = Severity.Error;

    [Required]
    public required string Message { get; set; }

    [Required]
    public required string StackTrace { get; set; }

    [Required]
    public required string Log { get; set; }
}
