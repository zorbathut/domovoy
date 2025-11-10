using Wumpus.Database;
using Wumpus.Shared.DTOs;
using Wumpus.Shared.Models;

namespace Wumpus.Intake.Services;

public class ReportService
{
    private readonly WumpusDbContext _context;
    private readonly ILogger<ReportService> _logger;

    public ReportService(WumpusDbContext context, ILogger<ReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> ProcessEventAsync(SubmitEventRequest request)
    {
        var now = DateTime.UtcNow;

        var eventReport = new Event
        {
            Id = Guid.NewGuid(),
            Timestamp = now,
            GameVersion = request.GameVersion,
            Platform = request.Platform,
            Data = new EventData
            {
                Name = request.Name,
                Category = request.Category,
                Value = request.Value,
                UserId = request.UserId,
                Metadata = request.Metadata
            }
        };

        _context.Events.Add(eventReport);
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
            Id = Guid.NewGuid(),
            Timestamp = now,
            GameVersion = request.GameVersion,
            Platform = request.Platform,
            Data = new ErrorData
            {
                Severity = request.Severity,
                Code = request.Code,
                Message = request.Message,
                ExceptionType = request.ExceptionType,
                StackTrace = request.StackTrace,
                Context = request.Context
            }
        };

        _context.Errors.Add(errorReport);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created error report {ReportId} with severity {Severity}: {Message}",
            errorReport.Id,
            errorReport.Data.Severity,
            errorReport.Data.Message);

        return errorReport.Id;
    }
}
