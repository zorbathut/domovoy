using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Wumpus.Database;
using Wumpus.Shared.DTOs;

namespace Wumpus.Web.Services;

public class CrashReportViewService
{
    private readonly WumpusDbContext _context;

    public CrashReportViewService(WumpusDbContext context)
    {
        _context = context;
    }

    public async Task<List<CrashReportResponse>> GetRecentCrashesAsync(int limit = 50)
    {
        var crashes = await _context.CrashReports
            .OrderByDescending(c => c.LastSeen)
            .Take(limit)
            .ToListAsync();

        return crashes.Select(MapToResponse).ToList();
    }

    public async Task<CrashReportResponse?> GetCrashByIdAsync(Guid id)
    {
        var crash = await _context.CrashReports.FindAsync(id);
        return crash != null ? MapToResponse(crash) : null;
    }

    public async Task<Dictionary<string, int>> GetCrashStatisticsAsync()
    {
        var totalCrashes = await _context.CrashReports.SumAsync(c => c.OccurrenceCount);
        var uniqueErrors = await _context.CrashReports.CountAsync();
        var platforms = await _context.CrashReports
            .GroupBy(c => c.Platform)
            .Select(g => new { Platform = g.Key, Count = g.Sum(c => c.OccurrenceCount) })
            .ToDictionaryAsync(x => x.Platform, x => x.Count);

        var stats = new Dictionary<string, int>
        {
            ["TotalCrashes"] = totalCrashes,
            ["UniqueErrors"] = uniqueErrors
        };

        foreach (var platform in platforms)
        {
            stats[$"Platform_{platform.Key}"] = platform.Value;
        }

        return stats;
    }

    public async Task<List<CrashReportResponse>> FilterCrashesAsync(
        string? platform = null,
        string? gameVersion = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.CrashReports.AsQueryable();

        if (!string.IsNullOrEmpty(platform))
            query = query.Where(c => c.Platform == platform);

        if (!string.IsNullOrEmpty(gameVersion))
            query = query.Where(c => c.GameVersion == gameVersion);

        if (startDate.HasValue)
            query = query.Where(c => c.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(c => c.Timestamp <= endDate.Value);

        var crashes = await query
            .OrderByDescending(c => c.LastSeen)
            .ToListAsync();

        return crashes.Select(MapToResponse).ToList();
    }

    private static CrashReportResponse MapToResponse(Wumpus.Shared.Models.CrashReport crash)
    {
        return new CrashReportResponse
        {
            Id = crash.Id,
            Timestamp = crash.Timestamp,
            GameVersion = crash.GameVersion,
            Platform = crash.Platform,
            ExceptionType = crash.ExceptionType,
            ExceptionMessage = crash.ExceptionMessage,
            StackTrace = crash.StackTrace,
            OccurrenceCount = crash.OccurrenceCount,
            FirstSeen = crash.FirstSeen,
            LastSeen = crash.LastSeen,
            SystemInfo = crash.SystemInfo != null
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(crash.SystemInfo)
                : null,
            UserContext = crash.UserContext != null
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(crash.UserContext)
                : null
        };
    }
}
