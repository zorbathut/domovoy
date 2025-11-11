using Microsoft.AspNetCore.Mvc;
using Domovoy.Web.Services;
using Domovoy.Shared.DTOs;

namespace Domovoy.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscribersController : ControllerBase
{
    private readonly SubscriberService _subscriberService;
    private readonly ILogger<SubscribersController> _logger;

    public SubscribersController(SubscriberService subscriberService, ILogger<SubscribersController> logger)
    {
        _subscriberService = subscriberService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<SubscriberResponse>> RegisterSubscriber([FromBody] RegisterSubscriberRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required" });

        var response = await _subscriberService.CreateSubscriberAsync(request);
        return CreatedAtAction(nameof(GetSubscriber), new { id = response.Id }, response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SubscriberResponse>> GetSubscriber(Guid id)
    {
        var subscriber = await _subscriberService.GetSubscriberAsync(id);
        if (subscriber == null)
            return NotFound();

        return Ok(subscriber);
    }

    [HttpGet]
    public async Task<ActionResult<List<SubscriberResponse>>> GetAllSubscribers()
    {
        var subscribers = await _subscriberService.GetAllSubscribersAsync();
        return Ok(subscribers);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SubscriberResponse>> UpdateSubscriber(Guid id, [FromBody] UpdateSubscriberRequest request)
    {
        var subscriber = await _subscriberService.UpdateSubscriberAsync(id, request);
        if (subscriber == null)
            return NotFound();

        return Ok(subscriber);
    }

    [HttpPost("{id}/heartbeat")]
    public async Task<ActionResult> Heartbeat(Guid id)
    {
        var success = await _subscriberService.HeartbeatAsync(id);
        if (!success)
            return NotFound();

        return Ok(new { message = "Heartbeat received" });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteSubscriber(Guid id)
    {
        var success = await _subscriberService.DeleteSubscriberAsync(id);
        if (!success)
            return NotFound();

        return NoContent();
    }
}
