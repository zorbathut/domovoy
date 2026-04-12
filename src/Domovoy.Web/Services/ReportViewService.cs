using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Domovoy.Database;
using Domovoy.Shared.DTOs;
using Domovoy.Shared.Models;

namespace Domovoy.Web.Services;

public record PagedResult<T>(List<T> Items, int TotalCount);

public class ReportViewService
{
    private readonly DomovoyDbContext _context;

    public ReportViewService(DomovoyDbContext context)
    {
        _context = context;
    }

    public async Task<Report?> GetReportByIdAsync(Guid id)
    {
        return await _context.Reports.FindAsync(id);
    }

    public async Task<Event?> GetEventByIdAsync(Guid id)
    {
        return await _context.Events.FindAsync(id);
    }

    public async Task<Error?> GetErrorByIdAsync(Guid id)
    {
        return await _context.Errors.FindAsync(id);
    }

    public async Task<Dictionary<string, int>> GetReportStatisticsAsync()
    {
        var totalReports = await _context.Reports.CountAsync();
        var totalEvents = await _context.Events.CountAsync();
        var totalErrors = await _context.Errors.CountAsync();

        var platforms = await _context.Reports
            .GroupBy(r => r.Platform)
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

    public async Task<PagedResult<Error>> GetErrorsPageAsync(
        int page,
        int pageSize,
        Severity? severity = null,
        string? platform = null,
        string? version = null,
        string? environment = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? userId = null,
        Guid? computerId = null,
        Guid? campaignId = null,
        Guid? processId = null)
    {
        var query = _context.Errors.AsQueryable();

        if (severity.HasValue)
            query = query.Where(e => e.Severity == severity.Value);

        if (!string.IsNullOrEmpty(platform))
            query = query.Where(e => EF.Functions.ILike(e.Platform, $"%{platform}%"));

        if (!string.IsNullOrEmpty(version))
            query = query.Where(e => EF.Functions.ILike(e.Version, $"%{version}%"));

        if (!string.IsNullOrEmpty(environment))
            query = query.Where(e => EF.Functions.ILike(e.Environment, $"%{environment}%"));

        if (startDate.HasValue)
            query = query.Where(e => e.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.Timestamp <= endDate.Value);

        if (userId.HasValue)
            query = query.Where(e => e.UserId == userId.Value);

        if (computerId.HasValue)
            query = query.Where(e => e.ComputerId == computerId.Value);

        if (campaignId.HasValue)
            query = query.Where(e => e.CampaignId == campaignId.Value);

        if (processId.HasValue)
            query = query.Where(e => e.ProcessId == processId.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(e => e.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Error>(items, total);
    }

    public async Task<PagedResult<Event>> GetEventsPageAsync(
        int page,
        int pageSize,
        string? category = null,
        string? platform = null,
        string? version = null,
        string? environment = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? userId = null,
        Guid? computerId = null,
        Guid? campaignId = null,
        Guid? processId = null)
    {
        var query = _context.Events.AsQueryable();

        if (!string.IsNullOrEmpty(category))
            query = query.Where(e => EF.Functions.ILike(e.Category, $"%{category}%"));

        if (!string.IsNullOrEmpty(platform))
            query = query.Where(e => EF.Functions.ILike(e.Platform, $"%{platform}%"));

        if (!string.IsNullOrEmpty(version))
            query = query.Where(e => EF.Functions.ILike(e.Version, $"%{version}%"));

        if (!string.IsNullOrEmpty(environment))
            query = query.Where(e => EF.Functions.ILike(e.Environment, $"%{environment}%"));

        if (startDate.HasValue)
            query = query.Where(e => e.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.Timestamp <= endDate.Value);

        if (userId.HasValue)
            query = query.Where(e => e.UserId == userId.Value);

        if (computerId.HasValue)
            query = query.Where(e => e.ComputerId == computerId.Value);

        if (campaignId.HasValue)
            query = query.Where(e => e.CampaignId == campaignId.Value);

        if (processId.HasValue)
            query = query.Where(e => e.ProcessId == processId.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(e => e.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Event>(items, total);
    }

    public async Task<List<Attachment>> GetAttachmentsByReportIdAsync(Guid reportId)
    {
        return await _context.Attachments
            .Where(a => a.ReportId == reportId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
    }
}
