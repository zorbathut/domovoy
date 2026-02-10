using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Domovoy.Database;
using Domovoy.Intake.Services;
using Domovoy.MinIO;

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

builder.Services.AddDbContext<DomovoyDbContext>(options =>
    options.UseNpgsql(dataSource));

// Add application services
builder.Services.AddScoped<ReportService>();
builder.Services.AddDomovoyMinio(builder.Configuration);

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DomovoyDbContext>();

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
    var db = scope.ServiceProvider.GetRequiredService<DomovoyDbContext>();
    await db.Database.MigrateAsync();
}

Log.Information("Domovoy Intake API starting...");
app.Run();
Log.CloseAndFlush();

// Make Program accessible to tests
namespace Domovoy.Intake
{
    public partial class Program { }
}
