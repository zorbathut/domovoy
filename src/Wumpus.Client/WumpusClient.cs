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
    /// <param name="name">Event name</param>
    /// <param name="category">Event category (default: "General")</param>
    /// <param name="value">Optional numeric value</param>
    /// <param name="userId">Optional user identifier</param>
    /// <param name="metadata">Optional event metadata</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The ID of the created event, or null if submission failed.</returns>
    public async Task<Guid?> SendEventAsync(
        string name,
        string category = "General",
        decimal? value = null,
        string? userId = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Event name is required", nameof(name));

        var request = new SubmitEventRequest
        {
            GameVersion = _options.AppVersion,
            Platform = _options.Platform,
            Name = name,
            Category = category,
            Value = value,
            UserId = userId,
            Metadata = metadata
        };

        return await SendRequestAsync("/api/v1/reports/event", request, cancellationToken);
    }

    /// <summary>
    /// Sends an error report.
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="severity">Error severity (Warning, Error, Critical, Fatal)</param>
    /// <param name="code">Optional error code</param>
    /// <param name="exceptionType">Optional exception type</param>
    /// <param name="stackTrace">Optional stack trace</param>
    /// <param name="context">Optional error context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The ID of the created error, or null if submission failed.</returns>
    public async Task<Guid?> SendErrorAsync(
        string message,
        string severity = "Error",
        string? code = null,
        string? exceptionType = null,
        string? stackTrace = null,
        string? context = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Error message is required", nameof(message));

        var request = new SubmitErrorRequest
        {
            GameVersion = _options.AppVersion,
            Platform = _options.Platform,
            Severity = severity,
            Code = code,
            Message = message.Length > 2000 ? message.Substring(0, 2000) : message,
            ExceptionType = exceptionType,
            StackTrace = stackTrace,
            Context = context
        };

        return await SendRequestAsync("/api/v1/reports/error", request, cancellationToken);
    }

    /// <summary>
    /// Sends a crash report for the specified exception.
    /// This is a convenience wrapper around SendErrorAsync with severity="Fatal".
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

        return await SendErrorAsync(
            message: exception.Message,
            severity: "Fatal",
            exceptionType: exception.GetType().FullName ?? exception.GetType().Name,
            stackTrace: exception.ToString(),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Sends an event without waiting for the result.
    /// Use this for fire-and-forget scenarios.
    /// </summary>
    public void SendEventFireAndForget(
        string name,
        string category = "General",
        decimal? value = null,
        string? userId = null,
        Dictionary<string, object>? metadata = null)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await SendEventAsync(name, category, value, userId, metadata);
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
    public void SendErrorFireAndForget(
        string message,
        string severity = "Error",
        string? code = null,
        string? exceptionType = null,
        string? stackTrace = null,
        string? context = null)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await SendErrorAsync(message, severity, code, exceptionType, stackTrace, context);
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
