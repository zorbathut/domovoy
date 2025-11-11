using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Wumpus.DiscordBot;

// Configure Serilog early so we can log startup errors
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Wumpus Discord Bot...");

    var builder = Host.CreateApplicationBuilder(args);

    // Configure Serilog from appsettings.json
    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services));

    // Register the Discord notification service
    builder.Services.AddHostedService<DiscordNotificationService>();

    var host = builder.Build();

    await host.RunAsync();

    Log.Information("Wumpus Discord Bot stopped gracefully");
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Wumpus Discord Bot terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
