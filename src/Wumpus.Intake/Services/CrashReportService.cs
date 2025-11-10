using Wumpus.Database;
using Wumpus.Shared.DTOs;
using Wumpus.Shared.Models;

namespace Wumpus.Intake.Services;

public class CrashReportService
{
    private readonly WumpusDbContext _context;
    private readonly ILogger<CrashReportService> _logger;

    public CrashReportService(WumpusDbContext context, ILogger<CrashReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> ProcessCrashReportAsync(SubmitCrashReportRequest request)
    {
        var now = DateTime.UtcNow;

        // Create new crash report - one row per crash
        var crashReport = new CrashReport
        {
            Id = Guid.NewGuid(),
            Timestamp = now,
            Core = request.Core
        };

        _context.CrashReports.Add(crashReport);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created new crash report {CrashId} for {ExceptionType}",
            crashReport.Id,
            crashReport.Core.ExceptionType);

        return crashReport.Id;
    }
}
