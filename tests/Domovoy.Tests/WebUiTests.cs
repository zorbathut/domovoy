using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUlid;
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
        _testContext.Services.AddScoped<DomovoyDbContext>(_ => dbContext);
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
            Id = Ulid.NewUlid().ToGuid(),
            Timestamp = DateTime.UtcNow,
            Standard = new StandardPayload
            {
                GameVersion = "1.5.0",
                Platform = "Windows",
                Environment = Environment.Dev,
                UserId = Ulid.NewUlid().ToGuid(),
                ComputerId = Ulid.NewUlid().ToGuid(),
                GameId = Ulid.NewUlid().ToGuid(),
                SequenceId = Ulid.NewUlid().ToGuid()
            },
            Data = new ErrorPayload
            {
                Severity = Severity.Fatal,
                Message = "Object reference not set",
                StackTrace = "at Game.Player.Move()",
                Log = "System.NullReferenceException: Object reference not set\nat Game.Player.Move()"
            }
        };
        dbContext.Errors.Add(error);
        await dbContext.SaveChangesAsync();

        // Act
        var cut = _testContext!.RenderComponent<Crashes>();
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
        var errorId = Ulid.NewUlid().ToGuid();
        var error = new Error
        {
            Id = errorId,
            Timestamp = DateTime.UtcNow,
            Standard = new StandardPayload
            {
                GameVersion = "2.0.0",
                Platform = "Linux",
                Environment = Environment.Release,
                UserId = Ulid.NewUlid().ToGuid(),
                ComputerId = Ulid.NewUlid().ToGuid(),
                GameId = Ulid.NewUlid().ToGuid(),
                SequenceId = Ulid.NewUlid().ToGuid()
            },
            Data = new ErrorPayload
            {
                Severity = Severity.Fatal,
                Message = "Invalid argument provided",
                StackTrace = "at Game.Combat.Attack()\nat Game.Player.DoAction()",
                Log = "System.ArgumentException: Invalid argument provided\nat Game.Combat.Attack()\nat Game.Player.DoAction()"
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
        markup.Should().Contain("Invalid argument provided");
        markup.Should().Contain("Linux");
        markup.Should().Contain("2.0.0");
        markup.Should().Contain("at Game.Combat.Attack()");
    }

    [Fact]
    public async Task CrashDetailPage_WithInvalidErrorId_ShowsNotFoundMessage()
    {
        // Arrange
        var nonExistentErrorId = Ulid.NewUlid().ToGuid();

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
                Id = Ulid.NewUlid().ToGuid(),
                Timestamp = DateTime.UtcNow,
                Standard = new StandardPayload
                {
                    GameVersion = "1.0.0",
                    Platform = "Windows",
                    Environment = Environment.Dev,
                    UserId = Ulid.NewUlid().ToGuid(),
                    ComputerId = Ulid.NewUlid().ToGuid(),
                    GameId = Ulid.NewUlid().ToGuid(),
                    SequenceId = Ulid.NewUlid().ToGuid()
                },
                Data = new ErrorPayload
                {
                    Severity = Severity.Fatal,
                    Message = "Null ref 1",
                    StackTrace = "stack1",
                    Log = "log1"
                }
            },
            new Error
            {
                Id = Ulid.NewUlid().ToGuid(),
                Timestamp = DateTime.UtcNow,
                Standard = new StandardPayload
                {
                    GameVersion = "1.0.0",
                    Platform = "Linux",
                    Environment = Environment.Dev,
                    UserId = Ulid.NewUlid().ToGuid(),
                    ComputerId = Ulid.NewUlid().ToGuid(),
                    GameId = Ulid.NewUlid().ToGuid(),
                    SequenceId = Ulid.NewUlid().ToGuid()
                },
                Data = new ErrorPayload
                {
                    Severity = Severity.Error,
                    Message = "Arg exception",
                    StackTrace = "stack2",
                    Log = "log2"
                }
            },
            new Error
            {
                Id = Ulid.NewUlid().ToGuid(),
                Timestamp = DateTime.UtcNow,
                Standard = new StandardPayload
                {
                    GameVersion = "2.0.0",
                    Platform = "macOS",
                    Environment = Environment.Release,
                    UserId = Ulid.NewUlid().ToGuid(),
                    ComputerId = Ulid.NewUlid().ToGuid(),
                    GameId = Ulid.NewUlid().ToGuid(),
                    SequenceId = Ulid.NewUlid().ToGuid()
                },
                Data = new ErrorPayload
                {
                    Severity = Severity.Fatal,
                    Message = "Invalid op",
                    StackTrace = "stack3",
                    Log = "log3"
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

        var error = new Error
        {
            Id = Ulid.NewUlid().ToGuid(),
            Timestamp = DateTime.UtcNow,
            Standard = new StandardPayload
            {
                GameVersion = "1.0.0",
                Platform = "Windows",
                UserId = Ulid.NewUlid().ToGuid(),
                ComputerId = Ulid.NewUlid().ToGuid(),
                GameId = Ulid.NewUlid().ToGuid(),
                SequenceId = Ulid.NewUlid().ToGuid()
            },
            Data = new ErrorPayload
            {
                Severity = Severity.Fatal,
                Message = "Test message",
                StackTrace = "Test stack",
                Log = "Test.Exception: Test message\nTest stack"
            }
        };
        dbContext.Errors.Add(error);
        await dbContext.SaveChangesAsync();

        // Act
        var results = await service.GetRecentErrorsAsync(10);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(1);
        results[0].Data.Message.Should().Be("Test message");
    }

    [Fact]
    public async Task ReportViewService_GetErrorByIdAsync_ReturnsError()
    {
        // Arrange
        await using var dbContext = _dbFixture.CreateDbContext();
        var service = new ReportViewService(dbContext);

        var errorId = Ulid.NewUlid().ToGuid();
        var error = new Error
        {
            Id = errorId,
            Timestamp = DateTime.UtcNow,
            Standard = new StandardPayload
            {
                GameVersion = "1.0.0",
                Platform = "Windows",
                UserId = Ulid.NewUlid().ToGuid(),
                ComputerId = Ulid.NewUlid().ToGuid(),
                GameId = Ulid.NewUlid().ToGuid(),
                SequenceId = Ulid.NewUlid().ToGuid()
            },
            Data = new ErrorPayload
            {
                Severity = Severity.Fatal,
                Message = "Test message",
                StackTrace = "Test stack",
                Log = "Test.Exception: Test message\nTest stack"
            }
        };
        dbContext.Errors.Add(error);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.GetErrorByIdAsync(errorId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(errorId);
        result.Data.Message.Should().Be("Test message");
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
                Id = Ulid.NewUlid().ToGuid(),
                Timestamp = DateTime.UtcNow,
                Standard = new StandardPayload
                {
                    GameVersion = "1.0.0",
                    Platform = "Windows",
                    Environment = Environment.Dev,
                    UserId = Ulid.NewUlid().ToGuid(),
                    ComputerId = Ulid.NewUlid().ToGuid(),
                    GameId = Ulid.NewUlid().ToGuid(),
                    SequenceId = Ulid.NewUlid().ToGuid()
                },
                Data = new ErrorPayload { Severity = Severity.Fatal, Message = "M1", StackTrace = "S1", Log = "L1" }
            },
            new Error
            {
                Id = Ulid.NewUlid().ToGuid(),
                Timestamp = DateTime.UtcNow,
                Standard = new StandardPayload
                {
                    GameVersion = "1.0.0",
                    Platform = "Linux",
                    Environment = Environment.Dev,
                    UserId = Ulid.NewUlid().ToGuid(),
                    ComputerId = Ulid.NewUlid().ToGuid(),
                    GameId = Ulid.NewUlid().ToGuid(),
                    SequenceId = Ulid.NewUlid().ToGuid()
                },
                Data = new ErrorPayload { Severity = Severity.Error, Message = "M2", StackTrace = "S2", Log = "L2" }
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
