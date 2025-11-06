using Microsoft.EntityFrameworkCore;
using Npgsql;
using Wumpus.Database;
using Xunit;

namespace Wumpus.Tests.Infrastructure;

/// <summary>
/// Fixture for managing the test database lifecycle.
/// Implements IAsyncLifetime to set up and tear down the database for each test collection.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private const string TestConnectionString = "Host=localhost;Database=wumpus_test;Username=wumpus;Password=wumpus";
    private const string AdminConnectionString = "Host=localhost;Database=postgres;Username=wumpus;Password=wumpus";

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
    /// Clears all data from the CrashReports table, useful for resetting state between tests.
    /// </summary>
    public async Task ClearCrashReportsAsync()
    {
        var options = new DbContextOptionsBuilder<WumpusDbContext>()
            .UseNpgsql(TestConnectionString)
            .Options;

        await using var context = new WumpusDbContext(options);
        context.CrashReports.RemoveRange(context.CrashReports);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets a new DbContext instance connected to the test database.
    /// </summary>
    public WumpusDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WumpusDbContext>()
            .UseNpgsql(TestConnectionString)
            .Options;

        return new WumpusDbContext(options);
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
                  WHERE pg_stat_activity.datname = 'wumpus_test'
                    AND pid <> pg_backend_pid();",
                connection);
            await terminateCmd.ExecuteNonQueryAsync();

            // Drop the database
            await using var dropCmd = new NpgsqlCommand("DROP DATABASE IF EXISTS wumpus_test;", connection);
            await dropCmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            // If database doesn't exist, that's fine
            Console.WriteLine($"Note: Could not drop database (may not exist): {ex.Message}");
        }
    }

    private async Task CreateDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync();

        await using var cmd = new NpgsqlCommand("CREATE DATABASE wumpus_test;", connection);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task RunMigrationsAsync()
    {
        var options = new DbContextOptionsBuilder<WumpusDbContext>()
            .UseNpgsql(TestConnectionString)
            .Options;

        await using var context = new WumpusDbContext(options);
        await context.Database.MigrateAsync();
    }
}
