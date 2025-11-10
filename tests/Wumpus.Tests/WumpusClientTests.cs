using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wumpus.Client;
using Wumpus.Tests.Infrastructure;
using Xunit;

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
        var options = new WumpusClientOptions
        {
            ServerUrl = _serverUrl,
            AppVersion = "1.5.0",
            Platform = "Windows",
            UserId = Guid.NewGuid(),
            ComputerId = Guid.NewGuid(),
            GameId = Guid.NewGuid()
        };

        var httpClient = _factory.CreateClient();
        using var client = new WumpusClient(options, httpClient);
        var exception = new InvalidOperationException("Test exception for crash reporting");

        // Act
        var success = await client.SendCrashAsync(exception);

        // Assert
        success.Should().BeTrue();
    }

    [Fact]
    public async Task SendCrashAsync_WithValidException_PersistsToDatabase()
    {
        // Arrange
        var options = new WumpusClientOptions
        {
            ServerUrl = _serverUrl,
            AppVersion = "2.0.0",
            Platform = "Linux",
            UserId = Guid.NewGuid(),
            ComputerId = Guid.NewGuid(),
            GameId = Guid.NewGuid()
        };

        var httpClient = _factory.CreateClient();
        using var client = new WumpusClient(options, httpClient);
        var exception = new ArgumentNullException("testParam", "Test parameter cannot be null");

        // Act
        var success = await client.SendCrashAsync(exception);

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
        var options = new WumpusClientOptions
        {
            ServerUrl = _serverUrl,
            AppVersion = "1.0.0",
            Platform = "Windows",
            UserId = Guid.NewGuid(),
            ComputerId = Guid.NewGuid(),
            GameId = Guid.NewGuid()
        };

        var httpClient = _factory.CreateClient();
        using var client = new WumpusClient(options, httpClient);

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
        var success1 = await client.SendCrashAsync(capturedException!);
        var success2 = await client.SendCrashAsync(capturedException!);

        // Assert - Both should succeed
        success1.Should().BeTrue();
        success2.Should().BeTrue();

        // Verify two separate records were created
        await using var dbContext = _dbFixture.CreateDbContext();
        var errorCount = await dbContext.Errors.CountAsync();

        errorCount.Should().Be(2);
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new WumpusClient(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithEmptyServerUrl_ThrowsArgumentException()
    {
        // Arrange
        var options = new WumpusClientOptions
        {
            ServerUrl = "",
            AppVersion = "1.0.0",
            Platform = "Windows",
            UserId = Guid.NewGuid(),
            ComputerId = Guid.NewGuid(),
            GameId = Guid.NewGuid()
        };

        // Act & Assert
        var act = () => new WumpusClient(options);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithEmptyAppVersion_ThrowsArgumentException()
    {
        // Arrange
        var options = new WumpusClientOptions
        {
            ServerUrl = "http://localhost",
            AppVersion = "",
            Platform = "Windows",
            UserId = Guid.NewGuid(),
            ComputerId = Guid.NewGuid(),
            GameId = Guid.NewGuid()
        };

        // Act & Assert
        var act = () => new WumpusClient(options);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithEmptyPlatform_ThrowsArgumentException()
    {
        // Arrange
        var options = new WumpusClientOptions
        {
            ServerUrl = "http://localhost",
            AppVersion = "1.0.0",
            Platform = "",
            UserId = Guid.NewGuid(),
            ComputerId = Guid.NewGuid(),
            GameId = Guid.NewGuid()
        };

        // Act & Assert
        var act = () => new WumpusClient(options);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("options");
    }

    [Fact]
    public async Task SendCrashAsync_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new WumpusClientOptions
        {
            ServerUrl = _serverUrl,
            AppVersion = "1.0.0",
            Platform = "Windows",
            UserId = Guid.NewGuid(),
            ComputerId = Guid.NewGuid(),
            GameId = Guid.NewGuid()
        };

        using var client = new WumpusClient(options);

        // Act & Assert
        var act = async () => await client.SendCrashAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("exception");
    }
}
