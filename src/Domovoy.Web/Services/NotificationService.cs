using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Domovoy.Database;
using Domovoy.Shared.DTOs;
using Domovoy.Shared.Models;

namespace Domovoy.Web.Services;

public class NotificationService
{
    private readonly DomovoyDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(DomovoyDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<NotificationResponse>> PullNotificationsAsync(
        Guid subscriberId,
        int limit = 100,
        string? workerId = null)
    {
        var lockDuration = TimeSpan.FromMinutes(5);
        var now = DateTime.UtcNow;
        var lockUntil = now.Add(lockDuration);
        workerId ??= System.Environment.MachineName;

        // Find available notifications (not locked or lock expired)
        var notificationIds = await _context.Notifications
            .Where(n => n.SubscriberId == subscriberId)
            .Where(n => n.LockedUntil == null || n.LockedUntil < now)
            .OrderBy(n => n.CreatedAt)
            .Take(limit)
            .Select(n => n.Id)
            .ToListAsync();

        if (!notificationIds.Any())
            return new List<NotificationResponse>();

        // Lock them atomically
        await _context.Notifications
            .Where(n => notificationIds.Contains(n.Id))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.LockedUntil, lockUntil)
                .SetProperty(n => n.LockedBy, workerId)
                .SetProperty(n => n.LastAttemptAt, now)
                .SetProperty(n => n.RetryCount, n => n.RetryCount + 1));

        // Fetch with report data
        var notifications = await _context.Notifications
            .Where(n => notificationIds.Contains(n.Id))
            .Include(n => n.Report)
            .ToListAsync();

        _logger.LogInformation(
            "Pulled {Count} notifications for subscriber {SubscriberId}",
            notifications.Count,
            subscriberId);

        return notifications.Select(MapToResponse).ToList();
    }

    public async Task<bool> AcknowledgeAsync(Guid notificationId)
    {
        var deleted = await _context.Notifications
            .Where(n => n.Id == notificationId)
            .ExecuteDeleteAsync();

        if (deleted > 0)
        {
            _logger.LogDebug("Acknowledged notification {NotificationId}", notificationId);
        }

        return deleted > 0;
    }

    private NotificationResponse MapToResponse(Notification notification)
    {
        var report = notification.Report;
        var reportDto = new ReportDto
        {
            Id = report.Id,
            Timestamp = report.Timestamp,
            Version = report.Version,
            Platform = report.Platform,
            Environment = report.Environment,
            UserId = report.UserId,
            ComputerId = report.ComputerId,
            CampaignId = report.CampaignId,
            CampaignSequenceIds = report.CampaignSequenceIds,
            ProcessId = report.ProcessId,
            Metadata = report.Metadata
        };

        if (report is Event eventReport)
        {
            reportDto.ReportType = "Event";
            reportDto.Category = eventReport.Category;
            reportDto.Name = eventReport.Name;
            reportDto.Data = eventReport.Data;
        }
        else if (report is Error errorReport)
        {
            reportDto.ReportType = "Error";
            reportDto.Severity = errorReport.Severity;
            reportDto.Message = errorReport.Message;
            reportDto.StackTrace = errorReport.StackTrace;
            reportDto.Log = errorReport.Log;
        }

        return new NotificationResponse
        {
            Id = notification.Id,
            ReportId = notification.ReportId,
            CreatedAt = notification.CreatedAt,
            RetryCount = notification.RetryCount,
            Report = reportDto
        };
    }
}
