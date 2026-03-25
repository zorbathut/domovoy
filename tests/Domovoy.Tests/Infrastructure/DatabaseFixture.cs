using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Domovoy.Database;
using Xunit;

namespace Domovoy.Tests.Infrastructure;

/// <summary>
/// Fixture for managing the test database and container lifecycle.
/// Spins up PostgreSQL and MinIO containers, then creates and migrates the test database.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private TestPostgresContainer? _postgres;
    private TestMinioContainer? _minio;

    /// <summary>Dynamic connection string set after PostgreSQL container starts.</summary>
    public static string TestConnectionString { get; private set; } = null!;

    /// <summary>Admin connection string (postgres database) for DDL operations.</summary>
    public static string AdminConnectionString { get; private set; } = null!;

    /// <summary>MinIO endpoint (localhost:port) set after MinIO container starts.</summary>
    public static string MinioEndpoint { get; private set; } = null!;

    /// <summary>MinIO access key.</summary>
    public static string MinioAccessKey { get; private set; } = null!;

    /// <summary>MinIO secret key.</summary>
    public static string MinioSecretKey { get; private set; } = null!;

    /// <summary>MinIO bucket name for test attachments.</summary>
    public static string MinioBucketName => "domovoy-test-attachments";

    private static NpgsqlDataSource CreateDataSource()
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(TestConnectionString);
        dataSourceBuilder.EnableDynamicJson();
        return dataSourceBuilder.Build();
    }

    public async Task InitializeAsync()
    {
        // Clean up stale containers from previous crashed runs
        await TestPostgresContainer.CleanupStaleContainersAsync();

        // Start both containers in parallel
        _postgres = new TestPostgresContainer();
        _minio = new TestMinioContainer();

        await Task.WhenAll(_postgres.StartAsync(), _minio.StartAsync());

        // Set static properties for factories to read
        TestConnectionString = _postgres.ConnectionString;
        AdminConnectionString = _postgres.AdminConnectionString;
        MinioEndpoint = _minio.Endpoint;
        MinioAccessKey = _minio.AccessKey;
        MinioSecretKey = _minio.SecretKey;

        // Spawn watchdog to clean up containers if the test process crashes
        TestPostgresContainer.SpawnWatchdog();

        // Create and migrate the test database
        await RunMigrationsAsync();
    }

    public async Task DisposeAsync()
    {
        if (_minio != null)
            await _minio.DisposeAsync();

        if (_postgres != null)
            await _postgres.DisposeAsync();
    }

    /// <summary>
    /// Clears all data from the Reports table, useful for resetting state between tests.
    /// </summary>
    public async Task ClearReportsAsync()
    {
        await using var dataSource = CreateDataSource();
        var options = new DbContextOptionsBuilder<DomovoyDbContext>()
            .UseNpgsql(dataSource)
            .Options;

        await using var context = new DomovoyDbContext(options);
        context.Reports.RemoveRange(context.Reports);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets a new DbContext instance connected to the test database.
    /// </summary>
    public DomovoyDbContext CreateDbContext()
    {
        var dataSource = CreateDataSource();
        var options = new DbContextOptionsBuilder<DomovoyDbContext>()
            .UseNpgsql(dataSource)
            .Options;

        return new DomovoyDbContext(options);
    }

    private async Task RunMigrationsAsync()
    {
        await using var dataSource = CreateDataSource();
        var options = new DbContextOptionsBuilder<DomovoyDbContext>()
            .UseNpgsql(dataSource)
            .Options;

        await using var context = new DomovoyDbContext(options);
        await context.Database.MigrateAsync();
    }
}
