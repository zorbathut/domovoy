using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Domovoy.Database;
using Domovoy.Shared.Models;
using Domovoy.Tests.Infrastructure;
using Domovoy.Web.Pages;
using Domovoy.Web.Services;
using Xunit;

namespace Domovoy.Tests;

/// <summary>
/// Integration tests for the Domovoy Web UI Blazor components.
/// Tests that pages render correctly and display data from the database.
/// </summary>
[Collection("Database")]
public class WebUiTests : IAsyncLifetime
{
    private readonly DatabaseFixture _dbFixture;
    private BunitContext? _testContext;

    public WebUiTests(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    public Task InitializeAsync()
    {
        _testContext = new BunitContext();

        // Register services needed by the Blazor components
        var dbContext = _dbFixture.CreateDbContext();
        _testContext.Services.AddScoped<DomovoyDbContext>(_ => dbContext);
        _testContext.Services.AddScoped<ReportViewService>();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _testContext?.Dispose();
        await _dbFixture.ClearReportsAsync();
    }

    private static Error CreateTestError(
        string version = "1.0.0",
        string platform = "Windows",
        string environment = "Dev",
        Severity severity = Severity.Fatal,
        string message = "Object reference not set",
        string stackTrace = "at Game.Player.Move()",
        string? log = null,
        Guid? id = null)
    {
        return new Error
        {
            Id = id ?? Guid.CreateVersion7(),
            Timestamp = DateTime.UtcNow,
            Version = version,
            Platform = platform,
            Environment = environment,
            UserId = Guid.CreateVersion7(),
            ComputerId = Guid.CreateVersion7(),
            CampaignId = Guid.CreateVersion7(),
            CampaignSequenceIds = [Guid.CreateVersion7()],
            ProcessId = Guid.CreateVersion7(),
            Severity = severity,
            Message = message,
            StackTrace = stackTrace,
            Log = log ?? $"System.NullReferenceException: {message}\n{stackTrace}"
        };
    }

    [Fact]
    public async Task CrashesPage_WithNoErrors_ShowsNoErrorsMessage()
    {
        // Arrange - Database is empty

        // Act
        var cut = _testContext!.Render<Crashes>();
        await Task.Delay(100); // Wait for async initialization

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("No errors found");
    }

    [Fact]
    public async Task CrashesPage_WithErrors_DisplaysErrorList()
    {
        // Arrange - Add an error to the database
        await using var dbContext = _dbFixture.CreateDbContext();
        var error = CreateTestError(
            version: "1.5.0",
            platform: "Windows",
            message: "Object reference not set",
            stackTrace: "at Game.Player.Move()");
        dbContext.Errors.Add(error);
        await dbContext.SaveChangesAsync();

        // Act
        var cut = _testContext!.Render<Crashes>();
        await Task.Delay(100); // Wait for async initialization

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Object reference not set");
        markup.Should().Contain("Windows");
        markup.Should().Contain("1.5.0");
    }

    [Fact]
    public async Task CrashesPage_ShowsLoadingMessage_DuringInitialization()
    {
        // Act
        var cut = _testContext!.Render<Crashes>();

        // Assert - Before async initialization completes
        var initialMarkup = cut.Markup;
        initialMarkup.Should().Contain("Loading errors");

        // Wait for async initialization to complete before disposal
        await Task.Delay(100);
    }

    [Fact]
    public async Task CrashDetailPage_WithValidErrorId_DisplaysErrorDetails()
    {
        // Arrange - Add an error to the database
        await using var dbContext = _dbFixture.CreateDbContext();
        var errorId = Guid.CreateVersion7();
        var error = CreateTestError(
            id: errorId,
            version: "2.0.0",
            platform: "Linux",
            environment: "Release",
            message: "Invalid argument provided",
            stackTrace: "at Game.Combat.Attack()\nat Game.Player.DoAction()");
        dbContext.Errors.Add(error);
        await dbContext.SaveChangesAsync();

        // Act
        var cut = _testContext!.Render<CrashDetail>(p => p.Add(c => c.CrashId, errorId));
        await Task.Delay(100); // Wait for async initialization

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain(errorId.ToString());
        markup.Should().Contain("Invalid argument provided");
        markup.Should().Contain("Linux");
        markup.Should().Contain("2.0.0");
        markup.Should().Contain("at Game.Combat.Attack()");
    }

    [Fact]
    public async Task CrashDetailPage_WithInvalidErrorId_ShowsNotFoundMessage()
    {
        // Arrange
        var nonExistentErrorId = Guid.CreateVersion7();

        // Act
        var cut = _testContext!.Render<CrashDetail>(p => p.Add(c => c.CrashId, nonExistentErrorId));
        await Task.Delay(100); // Wait for async initialization

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Error Report Not Found");
        markup.Should().Contain("could not be found");
    }

    [Fact]
    public async Task CrashesPage_WithMultipleErrors_DisplaysAllErrors()
    {
        // Arrange - Add multiple errors
        await using var dbContext = _dbFixture.CreateDbContext();

        var errors = new[]
        {
            CreateTestError(platform: "Windows", message: "Null ref 1"),
            CreateTestError(platform: "Linux", severity: Severity.Error, message: "Arg exception"),
            CreateTestError(version: "2.0.0", platform: "macOS", environment: "Release", message: "Invalid op")
        };

        dbContext.Errors.AddRange(errors);
        await dbContext.SaveChangesAsync();

        // Act
        var cut = _testContext!.Render<Crashes>();
        await Task.Delay(100);

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Null ref 1");
        markup.Should().Contain("Arg exception");
        markup.Should().Contain("Invalid op");
        markup.Should().Contain("Windows");
        markup.Should().Contain("Linux");
        markup.Should().Contain("macOS");
    }

    [Fact]
    public async Task ReportViewService_GetErrorsPageAsync_ReturnsRecentErrors()
    {
        // Arrange
        await using var dbContext = _dbFixture.CreateDbContext();
        var service = new ReportViewService(dbContext);

        var error = CreateTestError(message: "Test message", stackTrace: "Test stack");
        dbContext.Errors.Add(error);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.GetErrorsPageAsync(page: 1, pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Items[0].Message.Should().Be("Test message");
    }

    [Fact]
    public async Task ReportViewService_GetErrorsPageAsync_PaginatesResults()
    {
        // Arrange
        await using var dbContext = _dbFixture.CreateDbContext();
        var service = new ReportViewService(dbContext);

        var baseTime = DateTime.UtcNow;
        for (int i = 0; i < 5; i++)
        {
            var e = CreateTestError(message: $"M{i}");
            e.Timestamp = baseTime.AddSeconds(-i); // newest first when ordered desc
            dbContext.Errors.Add(e);
        }
        await dbContext.SaveChangesAsync();

        // Act
        var page1 = await service.GetErrorsPageAsync(page: 1, pageSize: 2);
        var page2 = await service.GetErrorsPageAsync(page: 2, pageSize: 2);
        var page3 = await service.GetErrorsPageAsync(page: 3, pageSize: 2);

        // Assert
        page1.TotalCount.Should().Be(5);
        page1.Items.Should().HaveCount(2);
        page2.Items.Should().HaveCount(2);
        page3.Items.Should().HaveCount(1);
        page1.Items[0].Message.Should().Be("M0");
        page2.Items[0].Message.Should().Be("M2");
        page3.Items[0].Message.Should().Be("M4");
    }

    [Fact]
    public async Task ReportViewService_GetErrorsPageAsync_FiltersByDateRange()
    {
        // Arrange
        await using var dbContext = _dbFixture.CreateDbContext();
        var service = new ReportViewService(dbContext);

        var now = DateTime.UtcNow;
        var old = CreateTestError(message: "old"); old.Timestamp = now.AddDays(-10);
        var mid = CreateTestError(message: "mid"); mid.Timestamp = now.AddDays(-5);
        var fresh = CreateTestError(message: "fresh"); fresh.Timestamp = now;
        dbContext.Errors.AddRange(old, mid, fresh);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.GetErrorsPageAsync(
            page: 1, pageSize: 50,
            startDate: now.AddDays(-7),
            endDate: now.AddDays(-1));

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items[0].Message.Should().Be("mid");
    }

    [Fact]
    public async Task ReportViewService_GetErrorByIdAsync_ReturnsError()
    {
        // Arrange
        await using var dbContext = _dbFixture.CreateDbContext();
        var service = new ReportViewService(dbContext);

        var errorId = Guid.CreateVersion7();
        var error = CreateTestError(id: errorId, message: "Test message", stackTrace: "Test stack");
        dbContext.Errors.Add(error);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.GetErrorByIdAsync(errorId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(errorId);
        result.Message.Should().Be("Test message");
    }

    [Fact]
    public async Task ReportViewService_GetErrorsPageAsync_FiltersByPlatform()
    {
        // Arrange
        await using var dbContext = _dbFixture.CreateDbContext();
        var service = new ReportViewService(dbContext);

        var errors = new[]
        {
            CreateTestError(platform: "Windows", message: "M1"),
            CreateTestError(platform: "Linux", severity: Severity.Error, message: "M2")
        };
        dbContext.Errors.AddRange(errors);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.GetErrorsPageAsync(page: 1, pageSize: 50, platform: "Windows");

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Platform.Should().Be("Windows");
    }

    [Fact]
    public async Task EventsPage_WithEvents_DisplaysEventList()
    {
        // Arrange
        await using var dbContext = _dbFixture.CreateDbContext();
        var ev = new Event
        {
            Id = Guid.CreateVersion7(),
            Timestamp = DateTime.UtcNow,
            Version = "1.2.3",
            Platform = "Windows",
            Environment = "Dev",
            UserId = Guid.CreateVersion7(),
            ComputerId = Guid.CreateVersion7(),
            CampaignId = Guid.CreateVersion7(),
            CampaignSequenceIds = [Guid.CreateVersion7()],
            ProcessId = Guid.CreateVersion7(),
            Category = "Gameplay",
            Name = "LevelCompleted"
        };
        dbContext.Events.Add(ev);
        await dbContext.SaveChangesAsync();

        // Act
        var cut = _testContext!.Render<Events>();
        await Task.Delay(100);

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("LevelCompleted");
        markup.Should().Contain("Gameplay");
        markup.Should().Contain("1.2.3");
    }

    [Fact]
    public async Task EventsPage_WithNoEvents_ShowsEmptyMessage()
    {
        // Act
        var cut = _testContext!.Render<Events>();
        await Task.Delay(100);

        // Assert
        cut.Markup.Should().Contain("No events found");
    }

    [Fact]
    public async Task EventDetailPage_WithValidId_DisplaysEventDetails()
    {
        // Arrange
        await using var dbContext = _dbFixture.CreateDbContext();
        var eventId = Guid.CreateVersion7();
        var ev = new Event
        {
            Id = eventId,
            Timestamp = DateTime.UtcNow,
            Version = "9.9.9",
            Platform = "macOS",
            Environment = "Release",
            UserId = Guid.CreateVersion7(),
            ComputerId = Guid.CreateVersion7(),
            CampaignId = Guid.CreateVersion7(),
            CampaignSequenceIds = [Guid.CreateVersion7()],
            ProcessId = Guid.CreateVersion7(),
            Category = "Tutorial",
            Name = "TutorialFinished"
        };
        dbContext.Events.Add(ev);
        await dbContext.SaveChangesAsync();

        // Act
        var cut = _testContext!.Render<EventDetail>(p => p.Add(c => c.EventId, eventId));
        await Task.Delay(100);

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain(eventId.ToString());
        markup.Should().Contain("TutorialFinished");
        markup.Should().Contain("Tutorial");
        markup.Should().Contain("macOS");
        markup.Should().Contain("9.9.9");
    }

    [Fact]
    public async Task ReportViewService_GetEventsPageAsync_PaginatesResults()
    {
        // Arrange
        await using var dbContext = _dbFixture.CreateDbContext();
        var service = new ReportViewService(dbContext);

        var baseTime = DateTime.UtcNow;
        for (int i = 0; i < 4; i++)
        {
            dbContext.Events.Add(new Event
            {
                Id = Guid.CreateVersion7(),
                Timestamp = baseTime.AddSeconds(-i),
                Version = "1",
                Platform = "P",
                Environment = "Dev",
                UserId = Guid.CreateVersion7(),
                ComputerId = Guid.CreateVersion7(),
                CampaignId = Guid.CreateVersion7(),
                CampaignSequenceIds = [Guid.CreateVersion7()],
                ProcessId = Guid.CreateVersion7(),
                Category = "C",
                Name = $"N{i}"
            });
        }
        await dbContext.SaveChangesAsync();

        // Act
        var page1 = await service.GetEventsPageAsync(page: 1, pageSize: 2);
        var page2 = await service.GetEventsPageAsync(page: 2, pageSize: 2);

        // Assert
        page1.TotalCount.Should().Be(4);
        page1.Items.Should().HaveCount(2);
        page2.Items.Should().HaveCount(2);
        page1.Items[0].Name.Should().Be("N0");
        page2.Items[0].Name.Should().Be("N2");
    }
}
