using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Domovoy.Database;

namespace Domovoy.Tests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for Domovoy Web UI integration tests.
/// Overrides the database connection to use the test database.
/// </summary>
public class WebUiFactory : WebApplicationFactory<Domovoy.Web.Program>
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
        });

        builder.UseEnvironment("Development");
    }
}
