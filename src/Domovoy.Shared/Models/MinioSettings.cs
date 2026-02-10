namespace Domovoy.Shared.Models;

public class MinioSettings
{
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = "domovoy";
    public string SecretKey { get; set; } = "domovoy123";
    public string BucketName { get; set; } = "domovoy-attachments";
    public bool UseSSL { get; set; } = false;
}
