using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Domovoy.Database;
using Domovoy.MinIO;
using Domovoy.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers(); // Add support for API controllers

// Add DbContext with Npgsql dynamic JSON support
var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<DomovoyDbContext>(options =>
    options.UseNpgsql(dataSource));

// Add application services
builder.Services.AddDomovoyMinio(builder.Configuration);
builder.Services.AddScoped<ReportViewService>();
builder.Services.AddScoped<SubscriberService>();
builder.Services.AddScoped<NotificationService>();

// Add background services
builder.Services.AddHostedService<LockCleanupService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DomovoyDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseSerilogRequestLogging();

app.UseStaticFiles();
app.UseRouting();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapHealthChecks("/health");

Log.Information("Domovoy Web UI starting...");
app.Run();
Log.CloseAndFlush();

// Make Program accessible to tests
namespace Domovoy.Web
{
    public partial class Program { }
}
