using Microsoft.AspNetCore.Mvc;
using Wumpus.Intake.Services;
using Wumpus.Shared.DTOs;

namespace Wumpus.Intake.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CrashesController : ControllerBase
{
    private readonly CrashReportService _crashReportService;
    private readonly ILogger<CrashesController> _logger;

    public CrashesController(
        CrashReportService crashReportService,
        ILogger<CrashesController> logger)
    {
        _crashReportService = crashReportService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit([FromBody] SubmitCrashReportRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var crashId = await _crashReportService.ProcessCrashReportAsync(request);

            return Accepted(new { id = crashId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing crash report");
            return StatusCode(500, new { error = "An error occurred processing the crash report" });
        }
    }
}
