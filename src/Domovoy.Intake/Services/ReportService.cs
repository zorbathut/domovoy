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
            Category = request.Category,
            Name = request.Name,
            Data = request.Data
        };
        MapCommonFields(eventReport, request);

        _context.Events.Add(eventReport);

        await CreateNotificationsForReportAsync(eventReport.Id, now);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created event report {ReportId} for {EventName}",
            eventReport.Id,
            eventReport.Name);

        return eventReport.Id;
    }

    public async Task<Guid> ProcessErrorAsync(SubmitErrorRequest request)
    {
        var now = DateTime.UtcNow;

        var errorReport = new Error
        {
            Id = Guid.CreateVersion7(),
            Timestamp = now,
            Severity = request.Severity,
            Message = request.Message,
            StackTrace = request.StackTrace,
            Log = request.Log
        };
        MapCommonFields(errorReport, request);

        _context.Errors.Add(errorReport);

        await CreateNotificationsForReportAsync(errorReport.Id, now);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created error report {ReportId} with severity {Severity}: {Message}",
            errorReport.Id,
            errorReport.Severity,
            errorReport.Message);

        return errorReport.Id;
    }

    private static void MapCommonFields(Report report, SubmitReportRequest request)
    {
        report.Version = request.Version;
        report.Platform = request.Platform;
        report.Environment = request.Environment;
        report.UserId = request.UserId;
        report.ComputerId = request.ComputerId;
        report.CampaignId = request.CampaignId;
        report.CampaignSequenceIds = request.CampaignSequenceIds;
        report.ProcessId = request.ProcessId;
        report.Metadata = request.Metadata;
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
