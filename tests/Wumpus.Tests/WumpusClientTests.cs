using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wumpus.Client;
using Wumpus.Shared.Models;
using Wumpus.Tests.Infrastructure;
using Xunit;
using Environment = Wumpus.Shared.Models.Environment;

namespace Wumpus.Tests;

/// <summary>
/// Integration tests for the Wumpus client library.
/// Tests end-to-end integration between the client and the Intake API.
/// </summary>
[Collection("Database")]
public class WumpusClientTests : IClassFixture<IntakeApiFactory>, IAsyncLifetime
{
    private readonly IntakeApiFactory _factory;
    private readonly DatabaseFixture _dbFixture;
    private readonly string _serverUrl;

    public WumpusClientTests(IntakeApiFactory factory, DatabaseFixture dbFixture)
    {
        _factory = factory;
        _dbFixture = dbFixture;

        // Get the test server URL from the factory
        var client = factory.CreateClient();
        _serverUrl = client.BaseAddress!.ToString();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _dbFixture.ClearReportsAsync();
    }

    [Fact]
    public async Task SendCrashAsync_WithValidException_ReturnsTrue()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        using var client = new WumpusClient(_serverUrl, httpClient);

        var standard = new StandardPayload
        {
            GameVersion = "1.5.0",
            Platform = "Windows",
            Environment = Environment.Dev,
            UserId = Guid.NewGuid(),
            ComputerId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            SequenceId = Guid.NewGuid()
        };

        var exception = new InvalidOperationException("Test exception for crash reporting");

        // Act
        var success = await client.SendCrashAsync(standard, exception);

        // Assert
        success.Should().BeTrue();
    }

    [Fact]
    public async Task SendCrashAsync_WithValidException_PersistsToDatabase()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        using var client = new WumpusClient(_serverUrl, httpClient);

        var standard = new StandardPayload
        {
            GameVersion = "2.0.0",
            Platform = "Linux",
            Environment = Environment.Release,
            UserId = Guid.NewGuid(),
            ComputerId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            SequenceId = Guid.NewGuid()
        };

        var exception = new ArgumentNullException("testParam", "Test parameter cannot be null");

        // Act
        var success = await client.SendCrashAsync(standard, exception);

        // Assert
        success.Should().BeTrue();

        await using var dbContext = _dbFixture.CreateDbContext();
        var savedError = await dbContext.Errors
            .Where(e => e.Standard.GameVersion == "2.0.0" && e.Standard.Platform == "Linux")
            .FirstOrDefaultAsync();

        savedError.Should().NotBeNull();
        savedError!.Data.Message.Should().Contain("Test parameter cannot be null");
        savedError.Data.StackTrace.Should().NotBeEmpty();
        savedError.Data.Log.Should().NotBeEmpty();
    }


    [Fact]
    public async Task SendCrashAsync_SameExceptionTwice_CreatesNewRecords()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        using var client = new WumpusClient(_serverUrl, httpClient);

        var standard = new StandardPayload
        {
            GameVersion = "1.0.0",
            Platform = "Windows",
            Environment = Environment.Dev,
            UserId = Guid.NewGuid(),
            ComputerId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            SequenceId = Guid.NewGuid()
        };

        // Create exception with a specific stack trace (by catching and re-throwing)
        Exception? capturedException = null;
        try
        {
            throw new InvalidOperationException("Test exception");
        }
        catch (Exception ex)
        {
            capturedException = ex;
        }

        // Act - Send the same exception twice
        var success1 = await client.SendCrashAsync(standard, capturedException!);
        var success2 = await client.SendCrashAsync(standard, capturedException!);

        // Assert - Both should succeed
        success1.Should().BeTrue();
        success2.Should().BeTrue();

        // Verify two separate records were created
        await using var dbContext = _dbFixture.CreateDbContext();
        var errorCount = await dbContext.Errors.CountAsync();

        errorCount.Should().Be(2);
    }

    [Fact]
    public void Constructor_WithNullServerUrl_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new WumpusClient(null!);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("serverUrl");
    }

    [Fact]
    public void Constructor_WithEmptyServerUrl_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new WumpusClient("");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("serverUrl");
    }

    [Fact]
    public async Task SendCrashAsync_WithNullStandardPayload_ThrowsArgumentNullException()
    {
        // Arrange
        using var client = new WumpusClient(_serverUrl);
        var exception = new InvalidOperationException("Test");

        // Act & Assert
        var act = async () => await client.SendCrashAsync(null!, exception);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("standard");
    }

    [Fact]
    public async Task SendCrashAsync_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        using var client = new WumpusClient(_serverUrl);

        var standard = new StandardPayload
        {
            GameVersion = "1.0.0",
            Platform = "Windows",
            Environment = Environment.Dev,
            UserId = Guid.NewGuid(),
            ComputerId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            SequenceId = Guid.NewGuid()
        };

        // Act & Assert
        var act = async () => await client.SendCrashAsync(standard, null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("exception");
    }

    [Fact]
    public async Task SendEventAsync_WithValidData_ReturnsTrue()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        using var client = new WumpusClient(_serverUrl, httpClient);

        var standard = new StandardPayload
        {
            GameVersion = "1.0.0",
            Platform = "Windows",
            Environment = Environment.Dev,
            UserId = Guid.NewGuid(),
            ComputerId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            SequenceId = Guid.NewGuid()
        };

        var eventData = new EventPayload
        {
            Name = "TestEvent",
            Category = "Testing",
            Value = 42,
            UserId = "test_user"
        };

        // Act
        var success = await client.SendEventAsync(standard, eventData);

        // Assert
        success.Should().BeTrue();
    }

    [Fact]
    public async Task SendErrorAsync_WithValidData_ReturnsTrue()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        using var client = new WumpusClient(_serverUrl, httpClient);

        var standard = new StandardPayload
        {
            GameVersion = "1.0.0",
            Platform = "Windows",
            Environment = Environment.Dev,
            UserId = Guid.NewGuid(),
            ComputerId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            SequenceId = Guid.NewGuid()
        };

        var errorData = new ErrorPayload
        {
            Severity = Severity.Error,
            Message = "Test error",
            StackTrace = "at TestMethod() in Test.cs:line 1",
            Log = "Test error log"
        };

        // Act
        var success = await client.SendErrorAsync(standard, errorData);

        // Assert
        success.Should().BeTrue();
    }
}
