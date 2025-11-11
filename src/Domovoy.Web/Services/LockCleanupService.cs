using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Domovoy.Database;

namespace Domovoy.Web.Services;

public class LockCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LockCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval;

    public LockCleanupService(IServiceProvider serviceProvider, ILogger<LockCleanupService> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Default to 1 minute cleanup interval
        var intervalMinutes = configuration.GetValue<int?>("NotificationOptions:CleanupIntervalMinutes") ?? 1;
        _cleanupInterval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Lock cleanup service starting with interval {Interval}", _cleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<DomovoyDbContext>();

                var now = DateTime.UtcNow;
                var released = await context.Notifications
                    .Where(n => n.LockedUntil != null && n.LockedUntil < now)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(n => n.LockedUntil, (DateTime?)null)
                        .SetProperty(n => n.LockedBy, (string?)null),
                        stoppingToken);

                if (released > 0)
                {
                    _logger.LogInformation("Released {Count} expired notification locks", released);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when shutting down
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in lock cleanup service");
            }
        }

        _logger.LogInformation("Lock cleanup service stopping");
    }
}
