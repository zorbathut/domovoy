using Microsoft.AspNetCore.Mvc;
using Wumpus.Intake.Services;
using Wumpus.Shared.DTOs;

namespace Wumpus.Intake.Controllers;

[ApiController]
[Route("api/v1/reports")]
public class ReportsController : ControllerBase
{
    private readonly ReportService _service;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(ReportService service, ILogger<ReportsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("event")]
    public async Task<IActionResult> SubmitEvent([FromBody] SubmitEventRequest request)
    {
        try
        {
            var id = await _service.ProcessEventAsync(request);
            return Accepted(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process event report");
            return StatusCode(500, new { error = "Failed to process event report" });
        }
    }

    [HttpPost("error")]
    public async Task<IActionResult> SubmitError([FromBody] SubmitErrorRequest request)
    {
        try
        {
            var id = await _service.ProcessErrorAsync(request);
            return Accepted(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process error report");
            return StatusCode(500, new { error = "Failed to process error report" });
        }
    }
}
