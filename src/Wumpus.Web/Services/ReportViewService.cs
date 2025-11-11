using Microsoft.EntityFrameworkCore;
using Wumpus.Database;
using Wumpus.Shared.DTOs;
using Wumpus.Shared.Models;

namespace Wumpus.Web.Services;

public class ReportViewService
{
    private readonly WumpusDbContext _context;

    public ReportViewService(WumpusDbContext context)
    {
        _context = context;
    }

    // Get recent reports of any type
    public async Task<List<Report>> GetRecentReportsAsync(ReportType? type = null, int limit = 50)
    {
        var query = _context.Reports.AsQueryable();

        if (type.HasValue)
            query = query.Where(r => r.ReportType == type.Value);

        return await query
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    // Get report by ID (any type)
    public async Task<Report?> GetReportByIdAsync(Guid id)
    {
        return await _context.Reports.FindAsync(id);
    }

    // Get recent events
    public async Task<List<Event>> GetRecentEventsAsync(int limit = 50)
    {
        return await _context.Events
            .OrderByDescending(e => e.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    // Get recent errors
    public async Task<List<Error>> GetRecentErrorsAsync(int limit = 50)
    {
        return await _context.Errors
            .OrderByDescending(e => e.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    // Get event by ID
    public async Task<Event?> GetEventByIdAsync(Guid id)
    {
        return await _context.Events.FindAsync(id);
    }

    // Get error by ID
    public async Task<Error?> GetErrorByIdAsync(Guid id)
    {
        return await _context.Errors.FindAsync(id);
    }

    // Get statistics
    public async Task<Dictionary<string, int>> GetReportStatisticsAsync()
    {
        var totalReports = await _context.Reports.CountAsync();
        var totalEvents = await _context.Events.CountAsync();
        var totalErrors = await _context.Errors.CountAsync();

        var platforms = await _context.Reports
            .GroupBy(r => r.Standard.Platform)
            .Select(g => new { Platform = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Platform, x => x.Count);

        var stats = new Dictionary<string, int>
        {
            ["TotalReports"] = totalReports,
            ["TotalEvents"] = totalEvents,
            ["TotalErrors"] = totalErrors
        };

        foreach (var platform in platforms)
        {
            stats[$"Platform_{platform.Key}"] = platform.Value;
        }

        return stats;
    }

    // Filter events
    public async Task<List<Event>> FilterEventsAsync(
        string? category = null,
        string? userId = null,
        string? platform = null,
        string? gameVersion = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.Events.AsQueryable();

        if (!string.IsNullOrEmpty(category))
            query = query.Where(e => e.Data.Category == category);

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(e => e.Data.UserId == userId);

        if (!string.IsNullOrEmpty(platform))
            query = query.Where(e => EF.Functions.ILike(e.Standard.Platform, $"%{platform}%"));

        if (!string.IsNullOrEmpty(gameVersion))
            query = query.Where(e => EF.Functions.ILike(e.Standard.GameVersion, $"%{gameVersion}%"));

        if (startDate.HasValue)
            query = query.Where(e => e.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.Timestamp <= endDate.Value);

        return await query
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync();
    }

    // Filter errors
    public async Task<List<Error>> FilterErrorsAsync(
        Severity? severity = null,
        string? platform = null,
        string? gameVersion = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.Errors.AsQueryable();

        if (severity.HasValue)
            query = query.Where(e => e.Data.Severity == severity.Value);

        if (!string.IsNullOrEmpty(platform))
            query = query.Where(e => EF.Functions.ILike(e.Standard.Platform, $"%{platform}%"));

        if (!string.IsNullOrEmpty(gameVersion))
            query = query.Where(e => EF.Functions.ILike(e.Standard.GameVersion, $"%{gameVersion}%"));

        if (startDate.HasValue)
            query = query.Where(e => e.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.Timestamp <= endDate.Value);

        return await query
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync();
    }
}
