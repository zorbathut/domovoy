using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Domovoy.Database;
using Domovoy.MinIO;
using Domovoy.Shared.Models;
using Minio;

namespace Domovoy.Tests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for Domovoy Intake API integration tests.
/// Overrides the database connection to use the test database.
/// </summary>
public class IntakeApiFactory : WebApplicationFactory<Domovoy.Intake.Program>
{
    private const string TestConnectionString = "Host=localhost;Database=domovoy_test;Username=domovoy;Password=domovoy";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DomovoyDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext with test database connection string and dynamic JSON support
            var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(TestConnectionString);
            dataSourceBuilder.EnableDynamicJson();
            var dataSource = dataSourceBuilder.Build();

            services.AddDbContext<DomovoyDbContext>(options =>
            {
                options.UseNpgsql(dataSource);
            });

            // Override MinIO settings to use localhost for test environment
            var minioDescriptors = services
                .Where(d => d.ServiceType == typeof(MinioSettings)
                          || d.ServiceType == typeof(IMinioClient)
                          || d.ServiceType == typeof(AttachmentStorageService))
                .ToList();

            foreach (var d in minioDescriptors)
                services.Remove(d);

            var testSettings = new MinioSettings
            {
                Endpoint = "localhost:9000",
                AccessKey = "domovoy",
                SecretKey = "domovoy123",
                BucketName = "domovoy-test-attachments",
                UseSSL = false
            };

            services.AddSingleton(testSettings);
            services.AddSingleton<IMinioClient>(_ =>
                new MinioClient()
                    .WithEndpoint(testSettings.Endpoint)
                    .WithCredentials(testSettings.AccessKey, testSettings.SecretKey)
                    .WithSSL(testSettings.UseSSL)
                    .Build());
            services.AddSingleton<AttachmentStorageService>();

            // Build service provider and ensure database is created and migrated
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DomovoyDbContext>();

            // The Intake API runs migrations on startup, so we don't need to do it here
            // But we ensure the connection works
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Development");
    }
}
