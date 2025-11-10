using System.ComponentModel.DataAnnotations;
using Wumpus.Shared.Models;

namespace Wumpus.Shared.DTOs;

public class SubmitEventRequest
{
    [Required]
    public required StandardPayload Standard { get; set; }

    [Required]
    public required EventPayload Data { get; set; }
}
