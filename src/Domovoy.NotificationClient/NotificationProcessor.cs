using Domovoy.Shared.DTOs;

namespace Domovoy.NotificationClient;

/// <summary>
/// Helper class for processing notifications with automatic heartbeat and acknowledgment
/// </summary>
public class NotificationProcessor
{
    private readonly DomovoyNotificationClient _client;
    private readonly TimeSpan _heartbeatInterval;
    private readonly TimeSpan _pollInterval;
    private readonly int _batchSize;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _heartbeatTask;

    public NotificationProcessor(
        DomovoyNotificationClient client,
        TimeSpan? heartbeatInterval = null,
        TimeSpan? pollInterval = null,
        int batchSize = 100)
    {
        _client = client;
        _heartbeatInterval = heartbeatInterval ?? TimeSpan.FromMinutes(5);
        _pollInterval = pollInterval ?? TimeSpan.FromSeconds(1);
        _batchSize = batchSize;
    }

    /// <summary>
    /// Starts processing notifications. Calls the handler for each notification and automatically acknowledges on success.
    /// </summary>
    /// <param name="handler">Handler function that processes a notification. Return true to ACK, false to skip ACK.</param>
    /// <param name="cancellationToken">Cancellation token to stop processing</param>
    public async Task StartAsync(Func<NotificationResponse, Task<bool>> handler, CancellationToken cancellationToken = default)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Start heartbeat background task
        _heartbeatTask = HeartbeatLoopAsync(_cancellationTokenSource.Token);

        // Main processing loop
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                var notifications = await _client.PullNotificationsAsync(_batchSize);

                foreach (var notification in notifications)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    try
                    {
                        var shouldAck = await handler(notification);
                        if (shouldAck)
                        {
                            await _client.AcknowledgeAsync(notification.Id);
                        }
                    }
                    catch
                    {
                        // Don't ACK if handler throws - notification will be retried
                        throw;
                    }
                }

                // Only delay if we didn't get a full batch (no need to delay if there might be more work)
                if (notifications.Count < _batchSize)
                {
                    await Task.Delay(_pollInterval, _cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        // Wait for heartbeat task to finish
        if (_heartbeatTask != null)
        {
            await _heartbeatTask;
        }
    }

    /// <summary>
    /// Stops the notification processor
    /// </summary>
    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
    }

    private async Task HeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_heartbeatInterval, cancellationToken);
                await _client.HeartbeatAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Ignore heartbeat failures - will retry next interval
            }
        }
    }
}
