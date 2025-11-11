using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wumpus.Database;
using Wumpus.Shared.DTOs;
using Wumpus.Shared.Models;
using Wumpus.Tests.Infrastructure;
using Xunit;

namespace Wumpus.Tests;

/// <summary>
/// Integration tests for the notification framework.
/// Tests subscriber management, notification creation, and consumption.
/// </summary>
[Collection("Database")]
public class NotificationFrameworkTests : IClassFixture<IntakeApiFactory>, IClassFixture<WebUiFactory>, IAsyncLifetime
{
    private readonly IntakeApiFactory _intakeFactory;
    private readonly WebUiFactory _webFactory;
    private readonly DatabaseFixture _dbFixture;
    private readonly HttpClient _intakeClient;
    private readonly HttpClient _webClient;

    public NotificationFrameworkTests(IntakeApiFactory intakeFactory, WebUiFactory webFactory, DatabaseFixture dbFixture)
    {
        _intakeFactory = intakeFactory;
        _webFactory = webFactory;
        _dbFixture = dbFixture;
        _intakeClient = intakeFactory.CreateClient();
        _webClient = webFactory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Clean up subscribers, notifications, and reports after each test
        await using var dbContext = _dbFixture.CreateDbContext();
        await dbContext.Notifications.ExecuteDeleteAsync();
        await dbContext.Subscribers.ExecuteDeleteAsync();
        await _dbFixture.ClearReportsAsync();
    }

    #region Subscriber Management Tests

    [Fact]
    public async Task RegisterSubscriber_WithValidData_ReturnsCreated()
    {
        // Arrange
        var request = new RegisterSubscriberRequest
        {
            Name = "Test Subscriber",
            HeartbeatTimeoutMinutes = 15
        };

        // Act
        var response = await _webClient.PostAsJsonAsync("/api/subscribers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var subscriber = await response.Content.ReadFromJsonAsync<SubscriberResponse>();
        subscriber.Should().NotBeNull();
        subscriber!.Name.Should().Be("Test Subscriber");
        subscriber.HeartbeatTimeoutMinutes.Should().Be(15);
        subscriber.IsActive.Should().BeTrue();
        subscriber.LastHeartbeat.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterSubscriber_WithValidData_PersistsToDatabase()
    {
        // Arrange
        var request = new RegisterSubscriberRequest
        {
            Name = "Database Test Subscriber",
            HeartbeatTimeoutMinutes = 10
        };

        // Act
        var response = await _webClient.PostAsJsonAsync("/api/subscribers", request);
        var subscriber = await response.Content.ReadFromJsonAsync<SubscriberResponse>();

        // Assert
        await using var dbContext = _dbFixture.CreateDbContext();
        var savedSubscriber = await dbContext.Subscribers.FindAsync(subscriber!.Id);
        savedSubscriber.Should().NotBeNull();
        savedSubscriber!.Name.Should().Be("Database Test Subscriber");
    }

    [Fact]
    public async Task Heartbeat_WithValidSubscriber_UpdatesLastHeartbeat()
    {
        // Arrange
        var subscriberId = await CreateTestSubscriberAsync();
        await Task.Delay(100); // Ensure time difference

        // Act
        var response = await _webClient.PostAsync($"/api/subscribers/{subscriberId}/heartbeat", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var dbContext = _dbFixture.CreateDbContext();
        var subscriber = await dbContext.Subscribers.FindAsync(subscriberId);
        subscriber!.LastHeartbeat.Should().NotBeNull();
        subscriber.LastHeartbeat!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetSubscriber_WithValidId_ReturnsSubscriber()
    {
        // Arrange
        var subscriberId = await CreateTestSubscriberAsync("Get Test");

        // Act
        var response = await _webClient.GetAsync($"/api/subscribers/{subscriberId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var subscriber = await response.Content.ReadFromJsonAsync<SubscriberResponse>();
        subscriber.Should().NotBeNull();
        subscriber!.Id.Should().Be(subscriberId);
        subscriber.Name.Should().Be("Get Test");
    }

    [Fact]
    public async Task UpdateSubscriber_WithValidData_UpdatesProperties()
    {
        // Arrange
        var subscriberId = await CreateTestSubscriberAsync();
        var updateRequest = new UpdateSubscriberRequest
        {
            IsActive = false,
            HeartbeatTimeoutMinutes = 20
        };

        // Act
        var response = await _webClient.PutAsJsonAsync($"/api/subscribers/{subscriberId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<SubscriberResponse>();
        updated!.IsActive.Should().BeFalse();
        updated.HeartbeatTimeoutMinutes.Should().Be(20);
    }

    [Fact]
    public async Task GetAllSubscribers_WithMultipleSubscribers_ReturnsAll()
    {
        // Arrange
        await CreateTestSubscriberAsync("Subscriber 1");
        await CreateTestSubscriberAsync("Subscriber 2");
        await CreateTestSubscriberAsync("Subscriber 3");

        // Act
        var response = await _webClient.GetAsync("/api/subscribers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var subscribers = await response.Content.ReadFromJsonAsync<List<SubscriberResponse>>();
        subscribers.Should().NotBeNull();
        subscribers!.Count.Should().BeGreaterOrEqualTo(3);
    }

    #endregion

    #region Notification Creation Tests

    [Fact]
    public async Task SubmitEvent_WithActiveSubscriber_CreatesNotification()
    {
        // Arrange
        var subscriberId = await CreateTestSubscriberAsync();
        var eventRequest = TestDataBuilder.CreateEventReport();

        // Act
        await _intakeClient.PostAsJsonAsync("/api/v1/reports/event", eventRequest);


        // Assert
        await using var dbContext = _dbFixture.CreateDbContext();
        var notifications = await dbContext.Notifications
            .Where(n => n.SubscriberId == subscriberId)
            .ToListAsync();

        notifications.Should().HaveCount(1);
        notifications[0].ReportId.Should().NotBeEmpty();
        notifications[0].CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        notifications[0].RetryCount.Should().Be(0);
    }

    [Fact]
    public async Task SubmitError_WithActiveSubscriber_CreatesNotification()
    {
        // Arrange
        var subscriberId = await CreateTestSubscriberAsync();
        var errorRequest = TestDataBuilder.CreateErrorReport();

        // Act
        await _intakeClient.PostAsJsonAsync("/api/v1/reports/error", errorRequest);


        // Assert
        await using var dbContext = _dbFixture.CreateDbContext();
        var notification = await dbContext.Notifications
            .Where(n => n.SubscriberId == subscriberId)
            .Include(n => n.Report)
            .FirstOrDefaultAsync();

        notification.Should().NotBeNull();
        notification!.Report.Should().BeOfType<Error>();
    }

    [Fact]
    public async Task SubmitReport_WithMultipleSubscribers_CreatesMultipleNotifications()
    {
        // Arrange
        var subscriber1 = await CreateTestSubscriberAsync("Subscriber 1");
        var subscriber2 = await CreateTestSubscriberAsync("Subscriber 2");
        var subscriber3 = await CreateTestSubscriberAsync("Subscriber 3");
        var eventRequest = TestDataBuilder.CreateEventReport();

        // Act
        await _intakeClient.PostAsJsonAsync("/api/v1/reports/event", eventRequest);


        // Assert
        await using var dbContext = _dbFixture.CreateDbContext();
        var notifications = await dbContext.Notifications.ToListAsync();
        notifications.Should().HaveCount(3);

        var subscriberIds = notifications.Select(n => n.SubscriberId).ToList();
        subscriberIds.Should().Contain(subscriber1);
        subscriberIds.Should().Contain(subscriber2);
        subscriberIds.Should().Contain(subscriber3);
    }

    [Fact]
    public async Task SubmitReport_WithExpiredHeartbeat_DoesNotCreateNotification()
    {
        // Arrange
        var subscriberId = await CreateTestSubscriberAsync(heartbeatTimeoutMinutes: 1);

        // Manually set LastHeartbeat to 2 minutes ago (expired)
        await using var dbContext = _dbFixture.CreateDbContext();
        var subscriber = await dbContext.Subscribers.FindAsync(subscriberId);
        subscriber!.LastHeartbeat = DateTime.UtcNow.AddMinutes(-2);
        await dbContext.SaveChangesAsync();

        var eventRequest = TestDataBuilder.CreateEventReport();

        // Act
        await _intakeClient.PostAsJsonAsync("/api/v1/reports/event", eventRequest);


        // Assert
        var notifications = await dbContext.Notifications
            .Where(n => n.SubscriberId == subscriberId)
            .ToListAsync();

        notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task SubmitReport_WithInactiveSubscriber_DoesNotCreateNotification()
    {
        // Arrange
        var subscriberId = await CreateTestSubscriberAsync();

        // Deactivate subscriber
        await _webClient.PutAsJsonAsync($"/api/subscribers/{subscriberId}",
            new UpdateSubscriberRequest { IsActive = false });

        var eventRequest = TestDataBuilder.CreateEventReport();

        // Act
        await _intakeClient.PostAsJsonAsync("/api/v1/reports/event", eventRequest);


        // Assert
        await using var dbContext = _dbFixture.CreateDbContext();
        var notifications = await dbContext.Notifications
            .Where(n => n.SubscriberId == subscriberId)
            .ToListAsync();

        notifications.Should().BeEmpty();
    }

    #endregion

    #region Notification Consumption Tests

    [Fact]
    public async Task PullNotifications_WithAvailableNotifications_ReturnsAndLocksNotifications()
    {
        // Arrange
        var subscriberId = await CreateTestSubscriberAsync();
        await SubmitTestReportsAsync(3);


        // Act
        var response = await _webClient.GetAsync($"/api/notifications?subscriberId={subscriberId}&limit=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationResponse>>();
        notifications.Should().HaveCount(3);

        // Verify they are locked in the database
        await using var dbContext = _dbFixture.CreateDbContext();
        var lockedNotifications = await dbContext.Notifications
            .Where(n => n.SubscriberId == subscriberId)
            .Where(n => n.LockedUntil != null)
            .ToListAsync();

        lockedNotifications.Should().HaveCount(3);
        lockedNotifications.All(n => n.RetryCount == 1).Should().BeTrue();
        lockedNotifications.All(n => n.LockedUntil > DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public async Task PullNotifications_IncludesFullReportData()
    {
        // Arrange
        var subscriberId = await CreateTestSubscriberAsync();
        var eventRequest = TestDataBuilder.CreateEventReport();
        await _intakeClient.PostAsJsonAsync("/api/v1/reports/event", eventRequest);


        // Act
        var response = await _webClient.GetAsync($"/api/notifications?subscriberId={subscriberId}&limit=10");
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationResponse>>();

        // Assert
        var notification = notifications!.First();
        notification.Report.Should().NotBeNull();
        notification.Report.ReportType.Should().Be("Event");
        notification.Report.EventData.Should().NotBeNull();
        notification.Report.EventData!.Name.Should().NotBeEmpty();
        notification.Report.Standard.Should().NotBeNull();
    }

    [Fact]
    public async Task PullNotifications_WithLockedNotifications_SkipsLockedOnes()
    {
        // Arrange
        var subscriberId = await CreateTestSubscriberAsync();
        await SubmitTestReportsAsync(5);


        // First pull locks some notifications
        await _webClient.GetAsync($"/api/notifications?subscriberId={subscriberId}&limit=3");

        // Act - Second pull should return only unlocked notifications
        var response = await _webClient.GetAsync($"/api/notifications?subscriberId={subscriberId}&limit=10");
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationResponse>>();

        // Assert
        notifications.Should().HaveCount(2); // Only 2 remaining unlocked
    }

    [Fact]
    public async Task PullNotifications_RespectsLimit()
    {
        // Arrange
        var subscriberId = await CreateTestSubscriberAsync();
        await SubmitTestReportsAsync(10);


        // Act
        var response = await _webClient.GetAsync($"/api/notifications?subscriberId={subscriberId}&limit=5");
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationResponse>>();

        // Assert
        notifications.Should().HaveCount(5);
    }

    [Fact]
    public async Task AcknowledgeNotification_WithValidId_DeletesNotification()
    {
        // Arrange
        var subscriberId = await CreateTestSubscriberAsync();
        await SubmitTestReportsAsync(1);


        var pullResponse = await _webClient.GetAsync($"/api/notifications?subscriberId={subscriberId}&limit=10");
        var notifications = await pullResponse.Content.ReadFromJsonAsync<List<NotificationResponse>>();
        var notificationId = notifications!.First().Id;

        // Act
        var response = await _webClient.DeleteAsync($"/api/notifications/{notificationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var dbContext = _dbFixture.CreateDbContext();
        var deleted = await dbContext.Notifications.FindAsync(notificationId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task AcknowledgeNotification_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _webClient.DeleteAsync($"/api/notifications/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AcknowledgeNotification_PostEndpoint_AlsoWorks()
    {
        // Arrange
        var subscriberId = await CreateTestSubscriberAsync();
        await SubmitTestReportsAsync(1);


        var pullResponse = await _webClient.GetAsync($"/api/notifications?subscriberId={subscriberId}&limit=10");
        var notifications = await pullResponse.Content.ReadFromJsonAsync<List<NotificationResponse>>();
        var notificationId = notifications!.First().Id;

        // Act
        var response = await _webClient.PostAsync($"/api/notifications/{notificationId}/ack", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var dbContext = _dbFixture.CreateDbContext();
        var deleted = await dbContext.Notifications.FindAsync(notificationId);
        deleted.Should().BeNull();
    }

    #endregion

    #region Lock Expiry Tests

    [Fact]
    public async Task PullNotifications_WithExpiredLock_ReturnsExpiredNotifications()
    {
        // Arrange
        var subscriberId = await CreateTestSubscriberAsync();
        await SubmitTestReportsAsync(1);


        // Pull and lock notification
        await _webClient.GetAsync($"/api/notifications?subscriberId={subscriberId}&limit=10");

        // Manually expire the lock
        await using var dbContext = _dbFixture.CreateDbContext();
        var notification = await dbContext.Notifications.FirstAsync();
        notification.LockedUntil = DateTime.UtcNow.AddMinutes(-1); // Expired 1 minute ago
        await dbContext.SaveChangesAsync();

        // Act - Should be able to pull again
        var response = await _webClient.GetAsync($"/api/notifications?subscriberId={subscriberId}&limit=10");
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationResponse>>();

        // Assert
        notifications.Should().HaveCount(1);
        notifications!.First().RetryCount.Should().Be(2); // Incremented on second pull
    }

    #endregion

    #region End-to-End Flow Tests

    [Fact]
    public async Task CompleteFlow_RegisterSubscriberToAcknowledgment_Works()
    {
        // 1. Register subscriber
        var registerResponse = await _webClient.PostAsJsonAsync("/api/subscribers",
            new RegisterSubscriberRequest { Name = "E2E Test", HeartbeatTimeoutMinutes = 10 });
        var subscriber = await registerResponse.Content.ReadFromJsonAsync<SubscriberResponse>();

        // 2. Send heartbeat
        var heartbeatResponse = await _webClient.PostAsync($"/api/subscribers/{subscriber!.Id}/heartbeat", null);
        heartbeatResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Submit report
        var eventRequest = TestDataBuilder.CreateEventReport();
        var submitResponse = await _intakeClient.PostAsJsonAsync("/api/v1/reports/event", eventRequest);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);


        // 4. Pull notifications
        var pullResponse = await _webClient.GetAsync($"/api/notifications?subscriberId={subscriber.Id}&limit=10");
        var notifications = await pullResponse.Content.ReadFromJsonAsync<List<NotificationResponse>>();
        notifications.Should().HaveCount(1);

        // 5. Acknowledge
        var ackResponse = await _webClient.DeleteAsync($"/api/notifications/{notifications!.First().Id}");
        ackResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 6. Verify notification is gone
        var verifyResponse = await _webClient.GetAsync($"/api/notifications?subscriberId={subscriber.Id}&limit=10");
        var verifyNotifications = await verifyResponse.Content.ReadFromJsonAsync<List<NotificationResponse>>();
        verifyNotifications.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private async Task<Guid> CreateTestSubscriberAsync(string name = "Test Subscriber", int heartbeatTimeoutMinutes = 10)
    {
        var request = new RegisterSubscriberRequest
        {
            Name = name,
            HeartbeatTimeoutMinutes = heartbeatTimeoutMinutes
        };

        var response = await _webClient.PostAsJsonAsync("/api/subscribers", request);
        var subscriber = await response.Content.ReadFromJsonAsync<SubscriberResponse>();
        return subscriber!.Id;
    }

    private async Task SubmitTestReportsAsync(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var eventRequest = TestDataBuilder.CreateEventReport();
            await _intakeClient.PostAsJsonAsync("/api/v1/reports/event", eventRequest);
        }
    }

    #endregion
}
