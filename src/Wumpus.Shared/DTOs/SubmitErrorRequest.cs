using System.ComponentModel.DataAnnotations;
using Wumpus.Shared.Models;

namespace Wumpus.Shared.DTOs;

public class SubmitErrorRequest
{
    [Required]
    [MaxLength(50)]
    public string GameVersion { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Platform { get; set; } = null!;

    [Required]
    public required ErrorPayload Data { get; set; }
}
