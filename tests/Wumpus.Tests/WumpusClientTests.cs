using FluentAssertions;
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
    public async Task SendCrashAsync_WithValidException_ReturnsCrashId()
    {
        // Arrange
        var options = new WumpusClientOptions
        {
            ServerUrl = _serverUrl,
            AppVersion = "1.5.0",
            Platform = "Windows"
        };

        var httpClient = _factory.CreateClient();
        using var client = new WumpusClient(options, httpClient);
        var exception = new InvalidOperationException("Test exception for crash reporting");

        // Act
        var crashId = await client.SendCrashAsync(exception);

        // Assert
        crashId.Should().NotBeNull();
        crashId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task SendCrashAsync_WithValidException_PersistsToDatabase()
    {
        // Arrange
        var options = new WumpusClientOptions
        {
            ServerUrl = _serverUrl,
            AppVersion = "2.0.0",
            Platform = "Linux"
        };

        var httpClient = _factory.CreateClient();
        using var client = new WumpusClient(options, httpClient);
        var exception = new ArgumentNullException("testParam", "Test parameter cannot be null");

        // Act
        var crashId = await client.SendCrashAsync(exception);

        // Assert
        crashId.Should().NotBeNull();

        await using var dbContext = _dbFixture.CreateDbContext();
        var savedError = await dbContext.Errors.FindAsync(crashId!.Value);

        savedError.Should().NotBeNull();
        savedError!.GameVersion.Should().Be("2.0.0");
        savedError.Platform.Should().Be("Linux");
        savedError.Data.ExceptionType.Should().Contain("ArgumentNullException");
        savedError.Data.Message.Should().Contain("Test parameter cannot be null");
        savedError.Data.StackTrace.Should().NotBeEmpty();
    }


    [Fact]
    public async Task SendCrashAsync_SameExceptionTwice_CreatesNewRecords()
    {
        // Arrange
        var options = new WumpusClientOptions
        {
            ServerUrl = _serverUrl,
            AppVersion = "1.0.0",
            Platform = "Windows"
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
        var crashId1 = await client.SendCrashAsync(capturedException!);
        var crashId2 = await client.SendCrashAsync(capturedException!);

        // Assert - Should create separate records even with identical exceptions
        crashId1.Should().NotBeNull();
        crashId2.Should().NotBeNull();
        crashId1.Should().NotBe(crashId2.Value);

        await using var dbContext = _dbFixture.CreateDbContext();
        var savedError1 = await dbContext.Errors.FindAsync(crashId1!.Value);
        var savedError2 = await dbContext.Errors.FindAsync(crashId2!.Value);

        savedError1.Should().NotBeNull();
        savedError2.Should().NotBeNull();
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
            Platform = "Windows"
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
            Platform = "Windows"
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
            Platform = ""
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
            Platform = "Windows"
        };

        using var client = new WumpusClient(options);

        // Act & Assert
        var act = async () => await client.SendCrashAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("exception");
    }
}
