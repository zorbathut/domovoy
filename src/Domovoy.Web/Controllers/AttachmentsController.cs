using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Domovoy.Database;
using Domovoy.MinIO;
using Domovoy.Shared.DTOs;

namespace Domovoy.Web.Controllers;

[ApiController]
[Route("api/attachments")]
public class AttachmentsController : ControllerBase
{
    private readonly DomovoyDbContext _context;
    private readonly AttachmentStorageService _storage;
    private readonly ILogger<AttachmentsController> _logger;

    public AttachmentsController(
        DomovoyDbContext context,
        AttachmentStorageService storage,
        ILogger<AttachmentsController> logger)
    {
        _context = context;
        _storage = storage;
        _logger = logger;
    }

    [HttpGet("by-report/{reportId:guid}")]
    public async Task<IActionResult> GetByReport(Guid reportId)
    {
        var attachments = await _context.Attachments
            .Where(a => a.ReportId == reportId)
            .OrderBy(a => a.CreatedAt)
            .Select(a => new AttachmentInfo
            {
                Id = a.Id,
                ReportId = a.ReportId,
                Filename = a.Filename,
                ContentType = a.ContentType,
                SizeBytes = a.SizeBytes,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return Ok(attachments);
    }

    [HttpGet("{attachmentId:guid}/download")]
    public async Task<IActionResult> Download(Guid attachmentId)
    {
        var attachment = await _context.Attachments.FindAsync(attachmentId);
        if (attachment == null)
            return NotFound();

        try
        {
            var stream = await _storage.GetDownloadStreamAsync(attachment.StorageKey);
            return File(stream, attachment.ContentType, attachment.Filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download attachment {AttachmentId}", attachmentId);
            return StatusCode(500);
        }
    }
}
