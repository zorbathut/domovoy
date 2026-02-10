using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Domovoy.Database;
using Domovoy.MinIO;
using Domovoy.Shared.DTOs;
using Domovoy.Shared.Models;
using NUlid;

namespace Domovoy.Intake.Controllers;

[ApiController]
[Route("api/v1/reports/{reportId:guid}/attachments")]
public class AttachmentsController : ControllerBase
{
    private readonly DomovoyDbContext _context;
    private readonly AttachmentStorageService _storage;
    private readonly ILogger<AttachmentsController> _logger;

    private const long MaxFileSize = 50L * 1024 * 1024; // 50 MB

    public AttachmentsController(
        DomovoyDbContext context,
        AttachmentStorageService storage,
        ILogger<AttachmentsController> logger)
    {
        _context = context;
        _storage = storage;
        _logger = logger;
    }

    [HttpPost]
    [RequestSizeLimit(50 * 1024 * 1024 + 1024)]
    public async Task<IActionResult> UploadAttachment(Guid reportId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "File is required and must not be empty." });

        if (file.Length > MaxFileSize)
            return BadRequest(new { error = $"File exceeds maximum size of {MaxFileSize / (1024 * 1024)} MB." });

        var reportExists = await _context.Reports.AnyAsync(r => r.Id == reportId);
        if (!reportExists)
            return NotFound(new { error = $"Report {reportId} not found." });

        var attachmentId = Ulid.NewUlid().ToGuid();
        var filename = file.FileName;
        var contentType = file.ContentType ?? "application/octet-stream";
        var storageKey = $"{reportId}/{attachmentId}/{filename}";

        try
        {
            using var stream = file.OpenReadStream();
            await _storage.UploadAsync(storageKey, stream, contentType, file.Length);

            var attachment = new Attachment
            {
                Id = attachmentId,
                ReportId = reportId,
                Filename = filename,
                ContentType = contentType,
                SizeBytes = file.Length,
                StorageKey = storageKey,
                CreatedAt = DateTime.UtcNow
            };

            _context.Attachments.Add(attachment);
            await _context.SaveChangesAsync();

            var info = new AttachmentInfo
            {
                Id = attachment.Id,
                ReportId = attachment.ReportId,
                Filename = attachment.Filename,
                ContentType = attachment.ContentType,
                SizeBytes = attachment.SizeBytes,
                CreatedAt = attachment.CreatedAt
            };

            _logger.LogInformation(
                "Uploaded attachment {AttachmentId} ({Filename}, {Size} bytes) for report {ReportId}",
                attachmentId, filename, file.Length, reportId);

            return Accepted(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload attachment for report {ReportId}", reportId);
            return StatusCode(500);
        }
    }
}
