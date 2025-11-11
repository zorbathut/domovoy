using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Domovoy.Shared.DTOs;
using Domovoy.Shared.Models;

namespace Domovoy.Client;

/// <summary>
/// Stateless client for sending event and error reports to a Domovoy reporting server.
/// </summary>
public class DomovoyClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;

    /// <summary>
    /// Creates a new instance of DomovoyClient with the specified server URL.
    /// </summary>
    /// <param name="serverUrl">The base URL of the Domovoy Intake API</param>
    /// <param name="timeoutSeconds">HTTP timeout in seconds (default: 30)</param>
    public DomovoyClient(string serverUrl, int timeoutSeconds = 30)
    {
        if (string.IsNullOrWhiteSpace(serverUrl))
            throw new ArgumentException("ServerUrl is required", nameof(serverUrl));

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(serverUrl.TrimEnd('/')),
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };
        _disposeHttpClient = true;
    }

    /// <summary>
    /// Creates a new instance of DomovoyClient with the specified server URL and HttpClient.
    /// Useful for testing scenarios.
    /// </summary>
    /// <param name="serverUrl">The base URL of the Domovoy Intake API</param>
    /// <param name="httpClient">Custom HttpClient instance</param>
    public DomovoyClient(string serverUrl, HttpClient httpClient)
    {
        if (string.IsNullOrWhiteSpace(serverUrl))
            throw new ArgumentException("ServerUrl is required", nameof(serverUrl));

        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.BaseAddress = new Uri(serverUrl.TrimEnd('/'));
        _disposeHttpClient = false;
    }

    /// <summary>
    /// Sends a game event.
    /// </summary>
    /// <param name="standard">Standard payload containing game version, platform, etc.</param>
    /// <param name="data">Event-specific data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if submission succeeded, false otherwise.</returns>
    public async Task<bool> SendEventAsync(
        StandardPayload standard,
        EventPayload data,
        CancellationToken cancellationToken = default)
    {
        if (standard == null)
            throw new ArgumentNullException(nameof(standard));
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        var request = new SubmitEventRequest
        {
            Standard = standard,
            Data = data
        };

        return await SendRequestAsync("/api/v1/reports/event", request, cancellationToken);
    }

    /// <summary>
    /// Sends an error report.
    /// </summary>
    /// <param name="standard">Standard payload containing game version, platform, etc.</param>
    /// <param name="data">Error-specific data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if submission succeeded, false otherwise.</returns>
    public async Task<bool> SendErrorAsync(
        StandardPayload standard,
        ErrorPayload data,
        CancellationToken cancellationToken = default)
    {
        if (standard == null)
            throw new ArgumentNullException(nameof(standard));
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        // Truncate message if needed
        if (data.Message != null && data.Message.Length > 2000)
            data.Message = data.Message.Substring(0, 2000);

        var request = new SubmitErrorRequest
        {
            Standard = standard,
            Data = data
        };

        return await SendRequestAsync("/api/v1/reports/error", request, cancellationToken);
    }

    /// <summary>
    /// Sends a crash report for the specified exception.
    /// This is a convenience method that builds an error report from the exception.
    /// </summary>
    /// <param name="standard">Standard payload containing game version, platform, etc.</param>
    /// <param name="exception">The exception to report</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if submission succeeded, false otherwise.</returns>
    public async Task<bool> SendCrashAsync(
        StandardPayload standard,
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        if (standard == null)
            throw new ArgumentNullException(nameof(standard));
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        var stackTrace = exception.StackTrace;
        if (string.IsNullOrWhiteSpace(stackTrace))
        {
            // If no stack trace, use the ToString() which includes type and message
            stackTrace = exception.ToString();
        }

        var data = new ErrorPayload
        {
            Severity = Severity.Fatal,
            Message = exception.Message,
            StackTrace = stackTrace,
            Log = exception.ToString()
        };

        return await SendErrorAsync(standard, data, cancellationToken);
    }

    /// <summary>
    /// Sends an event without waiting for the result.
    /// Use this for fire-and-forget scenarios.
    /// </summary>
    public void SendEventFireAndForget(StandardPayload standard, EventPayload data)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await SendEventAsync(standard, data);
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
    public void SendErrorFireAndForget(StandardPayload standard, ErrorPayload data)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await SendErrorAsync(standard, data);
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
    public void SendCrashFireAndForget(StandardPayload standard, Exception exception)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await SendCrashAsync(standard, exception);
            }
            catch
            {
                // Silently fail
            }
        });
    }

    private async Task<bool> SendRequestAsync<T>(
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

            return response.IsSuccessStatusCode;
        }
        catch
        {
            // Silently fail - we don't want reporting to crash the app
            return false;
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
