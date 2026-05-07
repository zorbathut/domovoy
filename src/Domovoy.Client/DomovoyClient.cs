using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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
    /// <returns>The report ID if submission succeeded, null otherwise.</returns>
    public async Task<Guid?> SendEventAsync(
        SubmitReportRequest common,
        string category,
        string name,
        Dictionary<string, object>? data = null,
        IReadOnlyList<FileAttachment>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        if (common == null)
            throw new ArgumentNullException(nameof(common));

        var request = new SubmitEventRequest
        {
            Version = common.Version,
            Platform = common.Platform,
            Environment = common.Environment,
            UserId = common.UserId,
            ComputerId = common.ComputerId,
            CampaignId = common.CampaignId,
            CampaignSequenceIds = common.CampaignSequenceIds,
            ProcessId = common.ProcessId,
            Metadata = common.Metadata,
            GeneratedAt = common.GeneratedAt,
            Category = category,
            Name = name,
            Data = data
        };

        var reportId = await SendRequestAsync("/api/v1/reports/event", request, cancellationToken);
        await UploadAttachmentsAsync(reportId, attachments, cancellationToken);
        return reportId;
    }

    /// <summary>
    /// Sends an error report.
    /// </summary>
    /// <returns>The report ID if submission succeeded, null otherwise.</returns>
    public async Task<Guid?> SendErrorAsync(
        SubmitReportRequest common,
        Severity severity,
        string message,
        string stackTrace,
        string log,
        IReadOnlyList<FileAttachment>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        if (common == null)
            throw new ArgumentNullException(nameof(common));

        // Truncate message if needed
        if (message.Length > 2000)
            message = message.Substring(0, 2000);

        var request = new SubmitErrorRequest
        {
            Version = common.Version,
            Platform = common.Platform,
            Environment = common.Environment,
            UserId = common.UserId,
            ComputerId = common.ComputerId,
            CampaignId = common.CampaignId,
            CampaignSequenceIds = common.CampaignSequenceIds,
            ProcessId = common.ProcessId,
            Metadata = common.Metadata,
            GeneratedAt = common.GeneratedAt,
            Severity = severity,
            Message = message,
            StackTrace = stackTrace,
            Log = log
        };

        var reportId = await SendRequestAsync("/api/v1/reports/error", request, cancellationToken);
        await UploadAttachmentsAsync(reportId, attachments, cancellationToken);
        return reportId;
    }

    /// <summary>
    /// Sends a crash report for the specified exception.
    /// This is a convenience method that builds an error report from the exception.
    /// </summary>
    /// <returns>The report ID if submission succeeded, null otherwise.</returns>
    public async Task<Guid?> SendCrashAsync(
        SubmitReportRequest common,
        Exception exception,
        IReadOnlyList<FileAttachment>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        if (common == null)
            throw new ArgumentNullException(nameof(common));
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        var stackTrace = exception.StackTrace;
        if (string.IsNullOrWhiteSpace(stackTrace))
        {
            stackTrace = exception.ToString();
        }

        return await SendErrorAsync(
            common,
            Severity.Fatal,
            exception.Message,
            stackTrace,
            exception.ToString(),
            attachments,
            cancellationToken);
    }

    /// <summary>
    /// Uploads an attachment for a previously submitted report.
    /// </summary>
    /// <returns>True if upload succeeded, false otherwise.</returns>
    public async Task<bool> SendAttachmentAsync(
        Guid reportId,
        Stream stream,
        string filename,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        if (string.IsNullOrWhiteSpace(filename))
            throw new ArgumentException("Filename is required", nameof(filename));

        try
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(contentType ?? "application/octet-stream");
            content.Add(streamContent, "file", filename);

            var response = await _httpClient.PostAsync(
                $"/api/v1/reports/{reportId}/attachments",
                content,
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            // Silently fail - we don't want reporting to crash the app
            return false;
        }
    }

    /// <summary>
    /// Sends an event without waiting for the result.
    /// </summary>
    public void SendEventFireAndForget(SubmitReportRequest common, string category, string name, Dictionary<string, object>? data = null, IReadOnlyList<FileAttachment>? attachments = null)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await SendEventAsync(common, category, name, data, attachments);
            }
            catch
            {
                // Silently fail
            }
        });
    }

    /// <summary>
    /// Sends an error without waiting for the result.
    /// </summary>
    public void SendErrorFireAndForget(SubmitReportRequest common, Severity severity, string message, string stackTrace, string log, IReadOnlyList<FileAttachment>? attachments = null)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await SendErrorAsync(common, severity, message, stackTrace, log, attachments);
            }
            catch
            {
                // Silently fail
            }
        });
    }

    /// <summary>
    /// Sends a crash report without waiting for the result.
    /// </summary>
    public void SendCrashFireAndForget(SubmitReportRequest common, Exception exception, IReadOnlyList<FileAttachment>? attachments = null)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await SendCrashAsync(common, exception, attachments);
            }
            catch
            {
                // Silently fail
            }
        });
    }

    /// <summary>
    /// Uploads an attachment without waiting for the result.
    /// </summary>
    public void SendAttachmentFireAndForget(Guid reportId, Stream stream, string filename, string? contentType = null)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await SendAttachmentAsync(reportId, stream, filename, contentType);
            }
            catch
            {
                // Silently fail
            }
        });
    }

    private async Task UploadAttachmentsAsync(
        Guid? reportId,
        IReadOnlyList<FileAttachment>? attachments,
        CancellationToken cancellationToken)
    {
        if (reportId == null || reportId == Guid.Empty || attachments == null || attachments.Count == 0)
            return;

        foreach (var attachment in attachments)
        {
            if (attachment?.Stream == null || string.IsNullOrWhiteSpace(attachment.Filename))
                continue;

            await SendAttachmentAsync(reportId.Value, attachment.Stream, attachment.Filename, attachment.ContentType, cancellationToken);
        }
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

            if (!response.IsSuccessStatusCode)
                return null;

            try
            {
                var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
                if (body.TryGetProperty("reportId", out var reportIdProp) &&
                    reportIdProp.TryGetGuid(out var reportId))
                {
                    return reportId;
                }
            }
            catch
            {
                // If we can't parse the body, still return a non-null value to indicate success
            }

            return Guid.Empty;
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
