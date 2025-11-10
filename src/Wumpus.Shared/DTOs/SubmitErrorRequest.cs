using System.ComponentModel.DataAnnotations;
using Wumpus.Shared.Models;

namespace Wumpus.Shared.DTOs;

public class SubmitErrorRequest
{
    [Required]
    public required StandardPayload Standard { get; set; }

    [Required]
    public required ErrorPayload Data { get; set; }
}
