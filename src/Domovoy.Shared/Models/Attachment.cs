using System;
using System.ComponentModel.DataAnnotations;

namespace Domovoy.Shared.Models;

public class Attachment
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }

    [Required]
    [MaxLength(255)]
    public required string Filename { get; set; }

    [Required]
    [MaxLength(100)]
    public required string ContentType { get; set; }

    public long SizeBytes { get; set; }

    [Required]
    [MaxLength(500)]
    public required string StorageKey { get; set; }

    public DateTime CreatedAt { get; set; }

    public Report Report { get; set; } = null!;
}
