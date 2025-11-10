using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wumpus.Database;
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
        _testContext.Services.AddScoped<ReportViewService>();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _testContext?.Dispose();
        await _dbFixture.ClearReportsAsync();
    }

    [Fact]
    public async Task CrashesPage_WithNoErrors_ShowsNoErrorsMessage()
    {
        // Arrange - Database is empty

        // Act
        var cut = _testContext!.RenderComponent<Crashes>();
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
        var error = new Error
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Standard = new StandardPayload
            {
                GameVersion = "1.5.0",
                Platform = "Windows",
                UserId = Guid.NewGuid(),
                ComputerId = Guid.NewGuid(),
                GameId = Guid.NewGuid(),
                SequenceId = Guid.NewGuid()
            },
            Data = new ErrorPayload
            {
                Severity = Severity.Fatal,
                ExceptionType = "System.NullReferenceException",
                Message = "Object reference not set",
                StackTrace = "at Game.Player.Move()"
            }
        };
        dbContext.Errors.Add(error);
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
        initialMarkup.Should().Contain("Loading errors");
    }

    [Fact]
    public async Task CrashDetailPage_WithValidErrorId_DisplaysErrorDetails()
    {
        // Arrange - Add an error to the database
        await using var dbContext = _dbFixture.CreateDbContext();
        var errorId = Guid.NewGuid();
        var error = new Error
        {
            Id = errorId,
            Timestamp = DateTime.UtcNow,
            Standard = new StandardPayload
            {
                GameVersion = "2.0.0",
                Platform = "Linux",
                UserId = Guid.NewGuid(),
                ComputerId = Guid.NewGuid(),
                GameId = Guid.NewGuid(),
                SequenceId = Guid.NewGuid()
            },
            Data = new ErrorPayload
            {
                Severity = Severity.Fatal,
                ExceptionType = "System.ArgumentException",
                Message = "Invalid argument provided",
                StackTrace = "at Game.Combat.Attack()\nat Game.Player.DoAction()"
            }
        };
        dbContext.Errors.Add(error);
        await dbContext.SaveChangesAsync();

        // Act
        var parameters = new[] { ComponentParameter.CreateParameter("CrashId", errorId) };
        var cut = _testContext!.RenderComponent<CrashDetail>(parameters);
        await Task.Delay(100); // Wait for async initialization

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain(errorId.ToString());
        markup.Should().Contain("System.ArgumentException");
        markup.Should().Contain("Invalid argument provided");
        markup.Should().Contain("Linux");
        markup.Should().Contain("2.0.0");
        markup.Should().Contain("at Game.Combat.Attack()");
    }

    [Fact]
    public async Task CrashDetailPage_WithInvalidErrorId_ShowsNotFoundMessage()
    {
        // Arrange
        var nonExistentErrorId = Guid.NewGuid();

        // Act
        var parameters = new[] { ComponentParameter.CreateParameter("CrashId", nonExistentErrorId) };
        var cut = _testContext!.RenderComponent<CrashDetail>(parameters);
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
            new Error
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Standard = new StandardPayload
                {
                    GameVersion = "1.0.0",
                    Platform = "Windows",
                    UserId = Guid.NewGuid(),
                    ComputerId = Guid.NewGuid(),
                    GameId = Guid.NewGuid(),
                    SequenceId = Guid.NewGuid()
                },
                Data = new ErrorPayload
                {
                    Severity = Severity.Fatal,
                    ExceptionType = "System.NullReferenceException",
                    Message = "Null ref 1",
                    StackTrace = "stack1"
                }
            },
            new Error
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Standard = new StandardPayload
                {
                    GameVersion = "1.0.0",
                    Platform = "Linux",
                    UserId = Guid.NewGuid(),
                    ComputerId = Guid.NewGuid(),
                    GameId = Guid.NewGuid(),
                    SequenceId = Guid.NewGuid()
                },
                Data = new ErrorPayload
                {
                    Severity = Severity.Error,
                    ExceptionType = "System.ArgumentException",
                    Message = "Arg exception",
                    StackTrace = "stack2"
                }
            },
            new Error
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Standard = new StandardPayload
                {
                    GameVersion = "2.0.0",
                    Platform = "macOS",
                    UserId = Guid.NewGuid(),
                    ComputerId = Guid.NewGuid(),
                    GameId = Guid.NewGuid(),
                    SequenceId = Guid.NewGuid()
                },
                Data = new ErrorPayload
                {
                    Severity = Severity.Fatal,
                    ExceptionType = "System.InvalidOperationException",
                    Message = "Invalid op",
                    StackTrace = "stack3"
                }
            }
        };

        dbContext.Errors.AddRange(errors);
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
    public async Task ReportViewService_GetRecentErrorsAsync_ReturnsRecentErrors()
    {
        // Arrange
        await using var dbContext = _dbFixture.CreateDbContext();
        var service = new ReportViewService(dbContext);

        var error = new Error
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Standard = new StandardPayload
            {
                GameVersion = "1.0.0",
                Platform = "Windows",
                UserId = Guid.NewGuid(),
                ComputerId = Guid.NewGuid(),
                GameId = Guid.NewGuid(),
                SequenceId = Guid.NewGuid()
            },
            Data = new ErrorPayload
            {
                Severity = Severity.Fatal,
                ExceptionType = "Test.Exception",
                Message = "Test message",
                StackTrace = "Test stack"
            }
        };
        dbContext.Errors.Add(error);
        await dbContext.SaveChangesAsync();

        // Act
        var results = await service.GetRecentErrorsAsync(10);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(1);
        results[0].Data.ExceptionType.Should().Be("Test.Exception");
    }

    [Fact]
    public async Task ReportViewService_GetErrorByIdAsync_ReturnsError()
    {
        // Arrange
        await using var dbContext = _dbFixture.CreateDbContext();
        var service = new ReportViewService(dbContext);

        var errorId = Guid.NewGuid();
        var error = new Error
        {
            Id = errorId,
            Timestamp = DateTime.UtcNow,
            Standard = new StandardPayload
            {
                GameVersion = "1.0.0",
                Platform = "Windows",
                UserId = Guid.NewGuid(),
                ComputerId = Guid.NewGuid(),
                GameId = Guid.NewGuid(),
                SequenceId = Guid.NewGuid()
            },
            Data = new ErrorPayload
            {
                Severity = Severity.Fatal,
                ExceptionType = "Test.Exception",
                Message = "Test message",
                StackTrace = "Test stack"
            }
        };
        dbContext.Errors.Add(error);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.GetErrorByIdAsync(errorId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(errorId);
        result.Data.ExceptionType.Should().Be("Test.Exception");
    }

    [Fact]
    public async Task ReportViewService_FilterErrorsAsync_FiltersByPlatform()
    {
        // Arrange
        await using var dbContext = _dbFixture.CreateDbContext();
        var service = new ReportViewService(dbContext);

        var errors = new[]
        {
            new Error
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Standard = new StandardPayload
                {
                    GameVersion = "1.0.0",
                    Platform = "Windows",
                    UserId = Guid.NewGuid(),
                    ComputerId = Guid.NewGuid(),
                    GameId = Guid.NewGuid(),
                    SequenceId = Guid.NewGuid()
                },
                Data = new ErrorPayload { Severity = Severity.Fatal, ExceptionType = "E1", Message = "M1", StackTrace = "S1" }
            },
            new Error
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Standard = new StandardPayload
                {
                    GameVersion = "1.0.0",
                    Platform = "Linux",
                    UserId = Guid.NewGuid(),
                    ComputerId = Guid.NewGuid(),
                    GameId = Guid.NewGuid(),
                    SequenceId = Guid.NewGuid()
                },
                Data = new ErrorPayload { Severity = Severity.Error, ExceptionType = "E2", Message = "M2", StackTrace = "S2" }
            }
        };
        dbContext.Errors.AddRange(errors);
        await dbContext.SaveChangesAsync();

        // Act
        var results = await service.FilterErrorsAsync(platform: "Windows");

        // Assert
        results.Should().HaveCount(1);
        results[0].Standard.Platform.Should().Be("Windows");
    }
}
