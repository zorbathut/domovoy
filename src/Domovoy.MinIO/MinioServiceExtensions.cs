using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using Domovoy.Shared.Models;

namespace Domovoy.MinIO;

public static class MinioServiceExtensions
{
    public static IServiceCollection AddDomovoyMinio(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = new MinioSettings();
        configuration.GetSection("Minio").Bind(settings);
        services.AddSingleton(settings);

        services.AddSingleton<IMinioClient>(_ =>
            new MinioClient()
                .WithEndpoint(settings.Endpoint)
                .WithCredentials(settings.AccessKey, settings.SecretKey)
                .WithSSL(settings.UseSSL)
                .Build());

        services.AddSingleton<AttachmentStorageService>();

        return services;
    }
}
