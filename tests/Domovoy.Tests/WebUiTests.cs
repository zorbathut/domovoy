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
using Environment = Domovoy.Shared.Models.Environment;

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
        Environment environment = Environment.Dev,
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
            environment: Environment.Release,
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
            CreateTestError(version: "2.0.0", platform: "macOS", environment: Environment.Release, message: "Invalid op")
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
    public async Task ReportViewService_GetRecentErrorsAsync_ReturnsRecentErrors()
    {
        // Arrange
        await using var dbContext = _dbFixture.CreateDbContext();
        var service = new ReportViewService(dbContext);

        var error = CreateTestError(message: "Test message", stackTrace: "Test stack");
        dbContext.Errors.Add(error);
        await dbContext.SaveChangesAsync();

        // Act
        var results = await service.GetRecentErrorsAsync(10);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(1);
        results[0].Message.Should().Be("Test message");
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
    public async Task ReportViewService_FilterErrorsAsync_FiltersByPlatform()
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
        var results = await service.FilterErrorsAsync(platform: "Windows");

        // Assert
        results.Should().HaveCount(1);
        results[0].Platform.Should().Be("Windows");
    }
}
