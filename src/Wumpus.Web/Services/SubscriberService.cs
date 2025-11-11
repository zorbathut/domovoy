using Microsoft.EntityFrameworkCore;
using NUlid;
using Wumpus.Database;
using Wumpus.Shared.DTOs;
using Wumpus.Shared.Models;

namespace Wumpus.Web.Services;

public class SubscriberService
{
    private readonly WumpusDbContext _context;
    private readonly ILogger<SubscriberService> _logger;

    public SubscriberService(WumpusDbContext context, ILogger<SubscriberService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SubscriberResponse> CreateSubscriberAsync(RegisterSubscriberRequest request)
    {
        var now = DateTime.UtcNow;

        var subscriber = new Subscriber
        {
            Id = Ulid.NewUlid().ToGuid(),
            Name = request.Name,
            IsActive = true,
            CreatedAt = now,
            LastHeartbeat = now, // Set initial heartbeat
            HeartbeatTimeoutMinutes = request.HeartbeatTimeoutMinutes ?? 10
        };

        _context.Subscribers.Add(subscriber);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created subscriber {SubscriberId} with name {Name}", subscriber.Id, subscriber.Name);

        return MapToResponse(subscriber);
    }

    public async Task<SubscriberResponse?> GetSubscriberAsync(Guid id)
    {
        var subscriber = await _context.Subscribers.FindAsync(id);
        return subscriber == null ? null : MapToResponse(subscriber);
    }

    public async Task<List<SubscriberResponse>> GetAllSubscribersAsync()
    {
        var subscribers = await _context.Subscribers
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

        return subscribers.Select(MapToResponse).ToList();
    }

    public async Task<SubscriberResponse?> UpdateSubscriberAsync(Guid id, UpdateSubscriberRequest request)
    {
        var subscriber = await _context.Subscribers.FindAsync(id);
        if (subscriber == null)
            return null;

        if (request.IsActive.HasValue)
            subscriber.IsActive = request.IsActive.Value;

        if (request.HeartbeatTimeoutMinutes.HasValue)
            subscriber.HeartbeatTimeoutMinutes = request.HeartbeatTimeoutMinutes.Value;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated subscriber {SubscriberId}", id);

        return MapToResponse(subscriber);
    }

    public async Task<bool> HeartbeatAsync(Guid id)
    {
        var subscriber = await _context.Subscribers.FindAsync(id);
        if (subscriber == null)
            return false;

        subscriber.LastHeartbeat = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogDebug("Heartbeat for subscriber {SubscriberId}", id);

        return true;
    }

    public async Task<bool> DeleteSubscriberAsync(Guid id)
    {
        var subscriber = await _context.Subscribers.FindAsync(id);
        if (subscriber == null)
            return false;

        _context.Subscribers.Remove(subscriber);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted subscriber {SubscriberId}", id);

        return true;
    }

    private static SubscriberResponse MapToResponse(Subscriber subscriber)
    {
        return new SubscriberResponse
        {
            Id = subscriber.Id,
            Name = subscriber.Name,
            IsActive = subscriber.IsActive,
            CreatedAt = subscriber.CreatedAt,
            LastHeartbeat = subscriber.LastHeartbeat,
            HeartbeatTimeoutMinutes = subscriber.HeartbeatTimeoutMinutes
        };
    }
}
