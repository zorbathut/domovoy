using System.IO;

namespace Domovoy.Client;

public class FileAttachment
{
    public required Stream Stream { get; init; }
    public required string Filename { get; init; }
    public string? ContentType { get; init; }
}
