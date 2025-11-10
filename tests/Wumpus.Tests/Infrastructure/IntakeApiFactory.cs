using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wumpus.Database;

namespace Wumpus.Tests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for Wumpus Intake API integration tests.
/// Overrides the database connection to use the test database.
/// </summary>
public class IntakeApiFactory : WebApplicationFactory<Wumpus.Intake.Program>
{
    private const string TestConnectionString = "Host=localhost;Database=wumpus_test;Username=wumpus;Password=wumpus";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<WumpusDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext with test database connection string and dynamic JSON support
            var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(TestConnectionString);
            dataSourceBuilder.EnableDynamicJson();
            var dataSource = dataSourceBuilder.Build();

            services.AddDbContext<WumpusDbContext>(options =>
            {
                options.UseNpgsql(dataSource);
            });

            // Build service provider and ensure database is created and migrated
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WumpusDbContext>();

            // The Intake API runs migrations on startup, so we don't need to do it here
            // But we ensure the connection works
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Development");
    }
}
