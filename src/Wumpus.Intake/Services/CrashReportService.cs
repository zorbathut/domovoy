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
        var stackTraceHash = ComputeStackTraceHash(request.StackTrace);
        var now = DateTime.UtcNow;

        // Check if we've seen this crash before
        var existingReport = await _context.CrashReports
            .FirstOrDefaultAsync(cr => cr.StackTraceHash == stackTraceHash);

        if (existingReport != null)
        {
            // Update existing report
            existingReport.OccurrenceCount++;
            existingReport.LastSeen = now;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Updated existing crash report {CrashId}. New occurrence count: {Count}",
                existingReport.Id,
                existingReport.OccurrenceCount);

            return existingReport.Id;
        }

        // Create new crash report
        var crashReport = new CrashReport
        {
            Id = Guid.NewGuid(),
            Timestamp = now,
            GameVersion = request.GameVersion,
            Platform = request.Platform,
            ExceptionType = request.ExceptionType,
            ExceptionMessage = request.ExceptionMessage,
            StackTrace = request.StackTrace,
            StackTraceHash = stackTraceHash,
            OccurrenceCount = 1,
            FirstSeen = now,
            LastSeen = now,
            SystemInfo = request.SystemInfo != null ? JsonSerializer.Serialize(request.SystemInfo) : null,
            UserContext = request.UserContext != null ? JsonSerializer.Serialize(request.UserContext) : null
        };

        _context.CrashReports.Add(crashReport);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created new crash report {CrashId} for {ExceptionType}",
            crashReport.Id,
            crashReport.ExceptionType);

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
