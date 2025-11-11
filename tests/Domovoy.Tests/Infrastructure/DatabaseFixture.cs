using Microsoft.EntityFrameworkCore;
using Npgsql;
using Domovoy.Database;
using Xunit;

namespace Domovoy.Tests.Infrastructure;

/// <summary>
/// Fixture for managing the test database lifecycle.
/// Implements IAsyncLifetime to set up and tear down the database for each test collection.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private const string TestConnectionString = "Host=localhost;Database=domovoy_test;Username=domovoy;Password=domovoy";
    private const string AdminConnectionString = "Host=localhost;Database=postgres;Username=domovoy;Password=domovoy";

    private static NpgsqlDataSource CreateDataSource()
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(TestConnectionString);
        dataSourceBuilder.EnableDynamicJson();
        return dataSourceBuilder.Build();
    }

    public async Task InitializeAsync()
    {
        // Drop and recreate the test database to ensure clean state
        await DropDatabaseIfExistsAsync();
        await CreateDatabaseAsync();
        await RunMigrationsAsync();
    }

    public async Task DisposeAsync()
    {
        // Optionally drop the database after tests complete
        // Comment this out if you want to inspect the database after test runs
        // await DropDatabaseIfExistsAsync();
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

    private async Task DropDatabaseIfExistsAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(AdminConnectionString);
            await connection.OpenAsync();

            // Terminate existing connections to the test database
            await using var terminateCmd = new NpgsqlCommand(
                @"SELECT pg_terminate_backend(pg_stat_activity.pid)
                  FROM pg_stat_activity
                  WHERE pg_stat_activity.datname = 'domovoy_test'
                    AND pid <> pg_backend_pid();",
                connection);
            await terminateCmd.ExecuteNonQueryAsync();

            // Drop the database
            await using var dropCmd = new NpgsqlCommand("DROP DATABASE IF EXISTS domovoy_test;", connection);
            await dropCmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            // If database doesn't exist, that's fine
            Console.WriteLine($"Note: Could not drop database (may not exist): {ex.Message}");
        }
        finally
        {
            // Clear Npgsql connection pool to prevent reusing terminated connections
            // This is critical because pg_terminate_backend() kills connections,
            // but Npgsql's connection pool doesn't know they're dead
            NpgsqlConnection.ClearAllPools();
        }
    }

    private async Task CreateDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync();

        await using var cmd = new NpgsqlCommand("CREATE DATABASE domovoy_test;", connection);
        await cmd.ExecuteNonQueryAsync();
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
