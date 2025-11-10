using Microsoft.EntityFrameworkCore;
using Serilog;
using Wumpus.Database;
using Wumpus.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add DbContext
builder.Services.AddDbContext<WumpusDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add application services
builder.Services.AddScoped<ReportViewService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<WumpusDbContext>();

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

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapHealthChecks("/health");

Log.Information("Wumpus Web UI starting...");
app.Run();
Log.CloseAndFlush();

// Make Program accessible to tests
namespace Wumpus.Web
{
    public partial class Program { }
}
