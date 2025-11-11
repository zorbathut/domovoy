using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Domovoy.Shared.DTOs;

namespace Domovoy.NotificationClient;

public class DomovoyNotificationClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly bool _disposeHttpClient;

    public Guid? SubscriberId { get; private set; }
    public string? WorkerId { get; set; }

    /// <summary>
    /// Creates a new notification client
    /// </summary>
    /// <param name="baseUrl">Base URL of the Domovoy Intake API (e.g., "http://localhost:1973")</param>
    /// <param name="httpClient">Optional HttpClient to use. If not provided, a new one will be created.</param>
    public DomovoyNotificationClient(string baseUrl, HttpClient? httpClient = null)
    {
        _baseUrl = baseUrl.TrimEnd('/');

        if (httpClient == null)
        {
            _httpClient = new HttpClient();
            _disposeHttpClient = true;
        }
        else
        {
            _httpClient = httpClient;
            _disposeHttpClient = false;
        }

        WorkerId = System.Environment.MachineName;
    }

    /// <summary>
    /// Registers this client as a subscriber
    /// </summary>
    /// <param name="name">Subscriber name</param>
    /// <param name="heartbeatTimeoutMinutes">Heartbeat timeout in minutes (default 10)</param>
    /// <returns>The subscriber ID</returns>
    public async Task<Guid> RegisterAsync(string name, int? heartbeatTimeoutMinutes = null)
    {
        var request = new RegisterSubscriberRequest
        {
            Name = name,
            HeartbeatTimeoutMinutes = heartbeatTimeoutMinutes
        };

        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/subscribers", request);
        response.EnsureSuccessStatusCode();

        var subscriberResponse = await response.Content.ReadFromJsonAsync<SubscriberResponse>();
        if (subscriberResponse == null)
            throw new InvalidOperationException("Failed to deserialize subscriber response");

        SubscriberId = subscriberResponse.Id;
        return subscriberResponse.Id;
    }

    /// <summary>
    /// Loads an existing subscriber by ID
    /// </summary>
    /// <param name="subscriberId">The subscriber ID</param>
    public async Task<bool> LoadSubscriberAsync(Guid subscriberId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/subscribers/{subscriberId}");
        if (!response.IsSuccessStatusCode)
            return false;

        var subscriberResponse = await response.Content.ReadFromJsonAsync<SubscriberResponse>();
        if (subscriberResponse == null)
            return false;

        SubscriberId = subscriberResponse.Id;
        return true;
    }

    /// <summary>
    /// Sends a heartbeat to keep the subscriber active
    /// </summary>
    public async Task<bool> HeartbeatAsync()
    {
        if (SubscriberId == null)
            throw new InvalidOperationException("Must register or load a subscriber before sending heartbeat");

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/subscribers/{SubscriberId}/heartbeat", null);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Pulls notifications for this subscriber
    /// </summary>
    /// <param name="limit">Maximum number of notifications to pull (default 100)</param>
    /// <returns>List of notifications</returns>
    public async Task<List<NotificationResponse>> PullNotificationsAsync(int limit = 100)
    {
        if (SubscriberId == null)
            throw new InvalidOperationException("Must register or load a subscriber before pulling notifications");

        var url = $"{_baseUrl}/api/notifications?subscriberId={SubscriberId}&limit={limit}";
        if (!string.IsNullOrEmpty(WorkerId))
            url += $"&workerId={Uri.EscapeDataString(WorkerId)}";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationResponse>>();
        return notifications ?? new List<NotificationResponse>();
    }

    /// <summary>
    /// Acknowledges a notification (deletes it)
    /// </summary>
    /// <param name="notificationId">The notification ID</param>
    /// <returns>True if successfully acknowledged</returns>
    public async Task<bool> AcknowledgeAsync(Guid notificationId)
    {
        var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/notifications/{notificationId}");
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Acknowledges multiple notifications in parallel
    /// </summary>
    /// <param name="notificationIds">The notification IDs to acknowledge</param>
    /// <returns>Number of successfully acknowledged notifications</returns>
    public async Task<int> AcknowledgeManyAsync(IEnumerable<Guid> notificationIds)
    {
        var tasks = notificationIds.Select(id => AcknowledgeAsync(id));
        var results = await Task.WhenAll(tasks);
        return results.Count(r => r);
    }

    /// <summary>
    /// Gets information about the subscriber
    /// </summary>
    public async Task<SubscriberResponse?> GetSubscriberInfoAsync()
    {
        if (SubscriberId == null)
            throw new InvalidOperationException("Must register or load a subscriber first");

        var response = await _httpClient.GetAsync($"{_baseUrl}/api/subscribers/{SubscriberId}");
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<SubscriberResponse>();
    }

    /// <summary>
    /// Updates the subscriber settings
    /// </summary>
    public async Task<bool> UpdateSubscriberAsync(bool? isActive = null, int? heartbeatTimeoutMinutes = null)
    {
        if (SubscriberId == null)
            throw new InvalidOperationException("Must register or load a subscriber first");

        var request = new UpdateSubscriberRequest
        {
            IsActive = isActive,
            HeartbeatTimeoutMinutes = heartbeatTimeoutMinutes
        };

        var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/api/subscribers/{SubscriberId}", request);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Deletes the subscriber (and all its pending notifications)
    /// </summary>
    public async Task<bool> DeleteSubscriberAsync()
    {
        if (SubscriberId == null)
            throw new InvalidOperationException("Must register or load a subscriber first");

        var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/subscribers/{SubscriberId}");
        if (response.IsSuccessStatusCode)
        {
            SubscriberId = null;
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
