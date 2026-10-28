namespace Domovoy.Client;

/// <summary>
/// Outcome of an attachment upload, with enough detail on failure to tell where it was rejected.
/// </summary>
public class AttachmentUploadResult
{
    public required bool Success { get; init; }

    /// <summary>
    /// HTTP status of the response, or null when none arrived (connection failure, timeout, reset mid-upload).
    /// </summary>
    public int? StatusCode { get; init; }

    /// <summary>
    /// The `Server` response header, which tells a reverse proxy's rejection apart from the Intake API's own.
    /// </summary>
    public string? Server { get; init; }

    /// <summary>
    /// On failure, the start of the response body or the full exception chain, truncated. Null on success.
    /// </summary>
    public string? Error { get; init; }
}
