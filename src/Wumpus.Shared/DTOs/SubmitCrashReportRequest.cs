using System.ComponentModel.DataAnnotations;
using Wumpus.Shared.Models;

namespace Wumpus.Shared.DTOs;

public class SubmitCrashReportRequest
{
    [Required]
    public required CrashReportCore Core { get; set; }
}
