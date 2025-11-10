using System.Net.Http.Json;
using System.Text.Json;
using Wumpus.Shared.DTOs;

namespace Wumpus.Client;

/// <summary>
/// Client for sending event and error reports to a Wumpus reporting server.
/// </summary>
public class WumpusClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly WumpusClientOptions _options;
    private readonly bool _disposeHttpClient;

    /// <summary>
    /// Creates a new instance of WumpusClient with the specified options.
    /// </summary>
    public WumpusClient(WumpusClientOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(options.ServerUrl))
            throw new ArgumentException("ServerUrl is required", nameof(options));

        if (string.IsNullOrWhiteSpace(options.AppVersion))
            throw new ArgumentException("AppVersion is required", nameof(options));

        if (string.IsNullOrWhiteSpace(options.Platform))
            throw new ArgumentException("Platform is required", nameof(options));

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(options.ServerUrl.TrimEnd('/')),
            Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
        };
        _disposeHttpClient = true;
    }

    /// <summary>
    /// Creates a new instance of WumpusClient with the specified options and HttpClient.
    /// Useful for testing scenarios.
    /// </summary>
    public WumpusClient(WumpusClientOptions options, HttpClient httpClient)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _disposeHttpClient = false;
    }

    /// <summary>
    /// Sends a game event.
    /// </summary>
    /// <param name="request">The event request (GameVersion and Platform will be set from client options)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The ID of the created event, or null if submission failed.</returns>
    public async Task<Guid?> SendEventAsync(
        SubmitEventRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Set client options
        request.GameVersion = _options.AppVersion;
        request.Platform = _options.Platform;

        return await SendRequestAsync("/api/v1/reports/event", request, cancellationToken);
    }

    /// <summary>
    /// Sends an error report.
    /// </summary>
    /// <param name="request">The error request (GameVersion and Platform will be set from client options)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The ID of the created error, or null if submission failed.</returns>
    public async Task<Guid?> SendErrorAsync(
        SubmitErrorRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Set client options
        request.GameVersion = _options.AppVersion;
        request.Platform = _options.Platform;

        // Truncate message if needed
        if (request.Message != null && request.Message.Length > 2000)
            request.Message = request.Message.Substring(0, 2000);

        return await SendRequestAsync("/api/v1/reports/error", request, cancellationToken);
    }

    /// <summary>
    /// Sends a crash report for the specified exception.
    /// This is a convenience method that builds an error report from the exception.
    /// </summary>
    /// <param name="exception">The exception to report</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The ID of the created crash report, or null if submission failed.</returns>
    public async Task<Guid?> SendCrashAsync(
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        var request = new SubmitErrorRequest
        {
            Severity = "Fatal",
            Message = exception.Message,
            ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
            StackTrace = exception.ToString()
        };

        return await SendErrorAsync(request, cancellationToken);
    }

    /// <summary>
    /// Sends an event without waiting for the result.
    /// Use this for fire-and-forget scenarios.
    /// </summary>
    public void SendEventFireAndForget(SubmitEventRequest request)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await SendEventAsync(request);
            }
            catch
            {
                // Silently fail
            }
        });
    }

    /// <summary>
    /// Sends an error without waiting for the result.
    /// Use this for fire-and-forget scenarios.
    /// </summary>
    public void SendErrorFireAndForget(SubmitErrorRequest request)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await SendErrorAsync(request);
            }
            catch
            {
                // Silently fail
            }
        });
    }

    /// <summary>
    /// Sends a crash report without waiting for the result.
    /// Use this for fire-and-forget scenarios.
    /// </summary>
    public void SendCrashFireAndForget(Exception exception)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await SendCrashAsync(exception);
            }
            catch
            {
                // Silently fail
            }
        });
    }

    private async Task<Guid?> SendRequestAsync<T>(
        string endpoint,
        T request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                endpoint,
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<JsonElement>(content);

                if (result.TryGetProperty("id", out var idProperty))
                {
                    return Guid.Parse(idProperty.GetString()!);
                }
            }

            return null;
        }
        catch
        {
            // Silently fail - we don't want reporting to crash the app
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient?.Dispose();
        }
    }
}
