using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Domovoy.Intake.Services;
using Domovoy.Shared.DTOs;

namespace Domovoy.Intake.Controllers;

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
            await _service.ProcessEventAsync(request);
            return Accepted();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process event report");
            return StatusCode(500);
        }
    }

    [HttpPost("error")]
    public async Task<IActionResult> SubmitError([FromBody] SubmitErrorRequest request)
    {
        try
        {
            await _service.ProcessErrorAsync(request);
            return Accepted();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process error report");
            return StatusCode(500);
        }
    }
}
