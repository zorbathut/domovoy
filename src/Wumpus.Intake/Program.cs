using Microsoft.EntityFrameworkCore;
using Serilog;
using Wumpus.Database;
using Wumpus.Intake.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add DbContext with Npgsql dynamic JSON support
var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<WumpusDbContext>(options =>
    options.UseNpgsql(dataSource));

// Add application services
builder.Services.AddScoped<ReportService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<WumpusDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSerilogRequestLogging();

app.MapControllers();
app.MapHealthChecks("/health");

// Run migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WumpusDbContext>();
    await db.Database.MigrateAsync();
}

Log.Information("Wumpus Intake API starting...");
app.Run();
Log.CloseAndFlush();

// Make Program accessible to tests
namespace Wumpus.Intake
{
    public partial class Program { }
}
