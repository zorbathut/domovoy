using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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
        var stackTraceHash = ComputeStackTraceHash(request.Core.StackTrace);
        var now = DateTime.UtcNow;

        // Check if we've seen this crash before
        var existingReport = await _context.CrashReports
            .FirstOrDefaultAsync(cr => cr.StackTraceHash == stackTraceHash);

        if (existingReport != null)
        {
            // Update existing report timestamp
            existingReport.Timestamp = now;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Updated existing crash report {CrashId}",
                existingReport.Id);

            return existingReport.Id;
        }

        // Create new crash report
        var crashReport = new CrashReport
        {
            Id = Guid.NewGuid(),
            Timestamp = now,
            Core = request.Core,
            StackTraceHash = stackTraceHash
        };

        _context.CrashReports.Add(crashReport);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created new crash report {CrashId} for {ExceptionType}",
            crashReport.Id,
            crashReport.Core.ExceptionType);

        return crashReport.Id;
    }

    private static string ComputeStackTraceHash(string stackTrace)
    {
        // Extract first 5 stack frames for hashing to group similar crashes
        var lines = stackTrace.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var framesToHash = string.Join('\n', lines.Take(5));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(framesToHash));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
