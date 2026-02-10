using System;

namespace Domovoy.Shared.DTOs;

public class AttachmentInfo
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public required string Filename { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}
