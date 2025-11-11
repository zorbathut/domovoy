using System.ComponentModel.DataAnnotations;
using Domovoy.Shared.Models;

namespace Domovoy.Shared.DTOs;

public class SubmitErrorRequest
{
    [Required]
    public required StandardPayload Standard { get; set; }

    [Required]
    public required ErrorPayload Data { get; set; }
}
