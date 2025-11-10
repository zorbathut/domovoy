using System.Net.Http.Json;
using System.Text.Json;
using Wumpus.Shared.DTOs;
using Wumpus.Shared.Models;

namespace Wumpus.Client;

/// <summary>
/// Client for sending crash reports to a Wumpus crash reporting server.
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
    /// <param name="options">The client options.</param>
    /// <param name="httpClient">The HttpClient to use for requests. Will not be disposed by this instance.</param>
    public WumpusClient(WumpusClientOptions options, HttpClient httpClient)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _disposeHttpClient = false;
    }

    /// <summary>
    /// Sends a crash report for the specified exception.
    /// </summary>
    /// <param name="exception">The exception to report.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created crash report, or null if submission failed.</returns>
    public async Task<Guid?> SendCrashReportAsync(
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        var request = new SubmitCrashReportRequest
        {
            Core = new CrashReportCore
            {
                GameVersion = _options.AppVersion,
                Platform = _options.Platform,
                ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
                ExceptionMessage = exception.Message.Length > 2000
                    ? exception.Message.Substring(0, 2000)
                    : exception.Message,
                StackTrace = exception.ToString()
            }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/v1/crashes",
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
            // Silently fail - we don't want crash reporting to crash the app
            return null;
        }
    }

    /// <summary>
    /// Sends a crash report without waiting for the result.
    /// Use this for fire-and-forget scenarios.
    /// </summary>
    public void SendCrashReportFireAndForget(Exception exception)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await SendCrashReportAsync(exception);
            }
            catch
            {
                // Silently fail
            }
        });
    }

    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient?.Dispose();
        }
    }
}
