using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wumpus.Database;
using Wumpus.Shared.DTOs;
using Wumpus.Shared.Models;
using Wumpus.Tests.Infrastructure;
using Wumpus.Web.Pages;
using Wumpus.Web.Services;
using Xunit;

namespace Wumpus.Tests;

/// <summary>
/// Integration tests for the Wumpus Web UI Blazor components.
/// Tests that pages render correctly and display data from the database.
/// </summary>
[Collection("Database")]
public class WebUiTests : IAsyncLifetime
{
    private readonly DatabaseFixture _dbFixture;
    private TestContext? _testContext;

    public WebUiTests(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    public Task InitializeAsync()
    {
        _testContext = new TestContext();

        // Register services needed by the Blazor components
        var dbContext = _dbFixture.CreateDbContext();
        _testContext.Services.AddScoped<WumpusDbContext>(_ => dbContext);
        _testContext.Services.AddScoped<CrashReportViewService>();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _testContext?.Dispose();
        await _dbFixture.ClearCrashReportsAsync();
    }

    [Fact]
    public async Task CrashesPage_WithNoCrashes_ShowsNoCrashesMessage()
    {
        // Arrange - Database is empty

        // Act
        var cut = _testContext!.RenderComponent<Crashes>();
        await Task.Delay(100); // Wait for async initialization

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("No crashes found");
    }

    [Fact]
    public async Task CrashesPage_WithCrashes_DisplaysCrashList()
    {
        // Arrange - Add a crash to the database
        await using var dbContext = _dbFixture.CreateDbContext();
        var crash = new CrashReport
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Core = new CrashReportCore
            {
                GameVersion = "1.5.0",
                Platform = "Windows",
                ExceptionType = "System.NullReferenceException",
                ExceptionMessage = "Object reference not set",
                StackTrace = "at Game.Player.Move()"
            }
        };
        dbContext.CrashReports.Add(crash);
        await dbContext.SaveChangesAsync();

        // Act
        var cut = _testContext!.RenderComponent<Crashes>();
        await Task.Delay(100); // Wait for async initialization

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("System.NullReferenceException");
        markup.Should().Contain("Windows");
        markup.Should().Contain("1.5.0");
    }

    [Fact]
    public async Task CrashesPage_ShowsLoadingMessage_DuringInitialization()
    {
        // Act
        var cut = _testContext!.RenderComponent<Crashes>();

        // Assert - Before async initialization completes
        var initialMarkup = cut.Markup;
        initialMarkup.Should().Contain("Loading crashes");
    }

    [Fact]
    public async Task CrashDetailPage_WithValidCrashId_DisplaysCrashDetails()
    {
        // Arrange - Add a crash to the database
        await using var dbContext = _dbFixture.CreateDbContext();
        var crashId = Guid.NewGuid();
        var crash = new CrashReport
        {
            Id = crashId,
            Timestamp = DateTime.UtcNow,
            Core = new CrashReportCore
            {
                GameVersion = "2.0.0",
                Platform = "Linux",
                ExceptionType = "System.ArgumentException",
                ExceptionMessage = "Invalid argument provided",
                StackTrace = "at Game.Combat.Attack()\nat Game.Player.DoAction()"
            }
        };
        dbContext.CrashReports.Add(crash);
        await dbContext.SaveChangesAsync();

        // Act
        var parameters = new[] { ComponentParameter.CreateParameter("CrashId", crashId) };
        var cut = _testContext!.RenderComponent<CrashDetail>(parameters);
        await Task.Delay(100); // Wait for async initialization

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain(crashId.ToString());
        markup.Should().Contain("System.ArgumentException");
        markup.Should().Contain("Invalid argument provided");
        markup.Should().Contain("Linux");
        markup.Should().Contain("2.0.0");
        markup.Should().Contain("at Game.Combat.Attack()");
    }

    [Fact]
    public async Task CrashDetailPage_WithInvalidCrashId_ShowsNotFoundMessage()
    {
        // Arrange
        var nonExistentCrashId = Guid.NewGuid();

        // Act
        var parameters = new[] { ComponentParameter.CreateParameter("CrashId", nonExistentCrashId) };
        var cut = _testContext!.RenderComponent<CrashDetail>(parameters);
        await Task.Delay(100); // Wait for async initialization

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Crash Report Not Found");
        markup.Should().Contain("could not be found");
    }

    [Fact]
    public async Task CrashesPage_WithMultipleCrashes_DisplaysAllCrashes()
    {
        // Arrange - Add multiple crashes
        await using var dbContext = _dbFixture.CreateDbContext();

        var crashes = new[]
        {
            new CrashReport
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Core = new CrashReportCore
                {
                    GameVersion = "1.0.0",
                    Platform = "Windows",
                    ExceptionType = "System.NullReferenceException",
                    ExceptionMessage = "Null ref 1",
                    StackTrace = "stack1"
                }
            },
            new CrashReport
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Core = new CrashReportCore
                {
                    GameVersion = "1.0.0",
                    Platform = "Linux",
                    ExceptionType = "System.ArgumentException",
                    ExceptionMessage = "Arg exception",
                    StackTrace = "stack2"
                }
            },
            new CrashReport
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Core = new CrashReportCore
                {
                    GameVersion = "2.0.0",
                    Platform = "macOS",
                    ExceptionType = "System.InvalidOperationException",
                    ExceptionMessage = "Invalid op",
                    StackTrace = "stack3"
                }
            }
        };

        dbContext.CrashReports.AddRange(crashes);
        await dbContext.SaveChangesAsync();

        // Act
        var cut = _testContext!.RenderComponent<Crashes>();
        await Task.Delay(100);

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("System.NullReferenceException");
        markup.Should().Contain("System.ArgumentException");
        markup.Should().Contain("System.InvalidOperationException");
        markup.Should().Contain("Windows");
        markup.Should().Contain("Linux");
        markup.Should().Contain("macOS");
    }

    [Fact]
    public async Task CrashReportViewService_GetRecentCrashesAsync_ReturnsRecentCrashes()
    {
        // Arrange
        await using var dbContext = _dbFixture.CreateDbContext();
        var service = new CrashReportViewService(dbContext);

        var crash = new CrashReport
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Core = new CrashReportCore
            {
                GameVersion = "1.0.0",
                Platform = "Windows",
                ExceptionType = "Test.Exception",
                ExceptionMessage = "Test message",
                StackTrace = "Test stack"
            }
        };
        dbContext.CrashReports.Add(crash);
        await dbContext.SaveChangesAsync();

        // Act
        var results = await service.GetRecentCrashesAsync(10);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(1);
        results[0].Core.ExceptionType.Should().Be("Test.Exception");
    }

    [Fact]
    public async Task CrashReportViewService_GetCrashByIdAsync_ReturnsCrash()
    {
        // Arrange
        await using var dbContext = _dbFixture.CreateDbContext();
        var service = new CrashReportViewService(dbContext);

        var crashId = Guid.NewGuid();
        var crash = new CrashReport
        {
            Id = crashId,
            Timestamp = DateTime.UtcNow,
            Core = new CrashReportCore
            {
                GameVersion = "1.0.0",
                Platform = "Windows",
                ExceptionType = "Test.Exception",
                ExceptionMessage = "Test message",
                StackTrace = "Test stack"
            }
        };
        dbContext.CrashReports.Add(crash);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.GetCrashByIdAsync(crashId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(crashId);
        result.Core.ExceptionType.Should().Be("Test.Exception");
    }

    [Fact]
    public async Task CrashReportViewService_FilterCrashesAsync_FiltersByPlatform()
    {
        // Arrange
        await using var dbContext = _dbFixture.CreateDbContext();
        var service = new CrashReportViewService(dbContext);

        var crashes = new[]
        {
            new CrashReport
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Core = new CrashReportCore { GameVersion = "1.0.0", Platform = "Windows", ExceptionType = "E1", ExceptionMessage = "M1", StackTrace = "S1" }
            },
            new CrashReport
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Core = new CrashReportCore { GameVersion = "1.0.0", Platform = "Linux", ExceptionType = "E2", ExceptionMessage = "M2", StackTrace = "S2" }
            }
        };
        dbContext.CrashReports.AddRange(crashes);
        await dbContext.SaveChangesAsync();

        // Act
        var results = await service.FilterCrashesAsync(platform: "Windows");

        // Assert
        results.Should().HaveCount(1);
        results[0].Core.Platform.Should().Be("Windows");
    }
}
