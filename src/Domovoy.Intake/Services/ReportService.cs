using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Domovoy.Database;
using Domovoy.Shared.DTOs;
using Domovoy.Shared.Models;
namespace Domovoy.Intake.Services;

public class ReportService
{
    private readonly DomovoyDbContext _context;
    private readonly ILogger<ReportService> _logger;

    public ReportService(DomovoyDbContext context, ILogger<ReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> ProcessEventAsync(SubmitEventRequest request)
    {
        var now = DateTime.UtcNow;

        var eventReport = new Event
        {
            Id = Guid.CreateVersion7(),
            Timestamp = now,
            Standard = request.Standard,
            Data = request.Data
        };

        _context.Events.Add(eventReport);

        // Create notifications for active subscribers (transactionally)
        await CreateNotificationsForReportAsync(eventReport.Id, now);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created event report {ReportId} for {EventName}",
            eventReport.Id,
            eventReport.Data.Name);

        return eventReport.Id;
    }

    public async Task<Guid> ProcessErrorAsync(SubmitErrorRequest request)
    {
        var now = DateTime.UtcNow;

        var errorReport = new Error
        {
            Id = Guid.CreateVersion7(),
            Timestamp = now,
            Standard = request.Standard,
            Data = request.Data
        };

        _context.Errors.Add(errorReport);

        // Create notifications for active subscribers (transactionally)
        await CreateNotificationsForReportAsync(errorReport.Id, now);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created error report {ReportId} with severity {Severity}: {Message}",
            errorReport.Id,
            errorReport.Data.Severity,
            errorReport.Data.Message);

        return errorReport.Id;
    }

    private async Task CreateNotificationsForReportAsync(Guid reportId, DateTime now)
    {
        // Find active subscribers with recent heartbeat
        var activeSubscribers = await _context.Subscribers
            .Where(s => s.IsActive)
            .Where(s => s.LastHeartbeat != null &&
                        s.LastHeartbeat.Value.AddMinutes(s.HeartbeatTimeoutMinutes) > now)
            .Select(s => s.Id)
            .ToListAsync();

        if (!activeSubscribers.Any())
            return;

        // Create notifications for each active subscriber
        var notifications = activeSubscribers.Select(subscriberId => new Notification
        {
            Id = Guid.CreateVersion7(),
            ReportId = reportId,
            SubscriberId = subscriberId,
            CreatedAt = now,
            RetryCount = 0
        }).ToList();

        _context.Notifications.AddRange(notifications);

        _logger.LogDebug(
            "Created {NotificationCount} notifications for report {ReportId}",
            notifications.Count,
            reportId);
    }
}
