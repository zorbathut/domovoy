using Microsoft.AspNetCore.Mvc;
using Domovoy.Web.Services;
using Domovoy.Shared.DTOs;

namespace Domovoy.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(NotificationService notificationService, ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificationResponse>>> PullNotifications(
        [FromQuery] Guid subscriberId,
        [FromQuery] int limit = 100,
        [FromQuery] string? workerId = null)
    {
        if (subscriberId == Guid.Empty)
            return BadRequest(new { error = "subscriberId is required" });

        if (limit < 1 || limit > 1000)
            return BadRequest(new { error = "limit must be between 1 and 1000" });

        var notifications = await _notificationService.PullNotificationsAsync(subscriberId, limit, workerId);
        return Ok(notifications);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> AcknowledgeNotification(Guid id)
    {
        var success = await _notificationService.AcknowledgeAsync(id);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpPost("{id}/ack")]
    public async Task<ActionResult> AcknowledgeNotificationPost(Guid id)
    {
        var success = await _notificationService.AcknowledgeAsync(id);
        if (!success)
            return NotFound();

        return Ok(new { message = "Notification acknowledged" });
    }
}
