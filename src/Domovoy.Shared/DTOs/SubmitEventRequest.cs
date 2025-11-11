using System.ComponentModel.DataAnnotations;
using Domovoy.Shared.Models;

namespace Domovoy.Shared.DTOs;

public class SubmitEventRequest
{
    [Required]
    public required StandardPayload Standard { get; set; }

    [Required]
    public required EventPayload Data { get; set; }
}
