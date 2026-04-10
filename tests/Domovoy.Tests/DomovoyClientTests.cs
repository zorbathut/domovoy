using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Domovoy.Client;
using Domovoy.Shared.DTOs;
using Domovoy.Shared.Models;
using Domovoy.Tests.Infrastructure;
using Xunit;
using Environment = Domovoy.Shared.Models.Environment;

namespace Domovoy.Tests;

/// <summary>
/// Integration tests for the Domovoy client library.
/// Tests end-to-end integration between the client and the Intake API.
/// </summary>
[Collection("Database")]
public class DomovoyClientTests : IClassFixture<IntakeApiFactory>, IAsyncLifetime
{
    private readonly IntakeApiFactory _factory;
    private readonly DatabaseFixture _dbFixture;
    private readonly string _serverUrl;

    public DomovoyClientTests(IntakeApiFactory factory, DatabaseFixture dbFixture)
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

    private static SubmitReportRequest CreateCommon(
        string version = "1.0.0",
        string platform = "Windows",
        Environment environment = Environment.Dev)
    {
        return new SubmitReportRequest
        {
            Version = version,
            Platform = platform,
            Environment = environment,
            UserId = Guid.CreateVersion7(),
            ComputerId = Guid.CreateVersion7(),
            CampaignId = Guid.CreateVersion7(),
            CampaignSequenceIds = [Guid.CreateVersion7()],
            ProcessId = Guid.CreateVersion7()
        };
    }

    [Fact]
    public async Task SendCrashAsync_WithValidException_ReturnsReportId()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        using var client = new DomovoyClient(_serverUrl, httpClient);
        var common = CreateCommon(version: "1.5.0");

        var exception = new InvalidOperationException("Test exception for crash reporting");

        // Act
        var reportId = await client.SendCrashAsync(common, exception);

        // Assert
        reportId.Should().NotBeNull();
        reportId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task SendCrashAsync_WithValidException_PersistsToDatabase()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        using var client = new DomovoyClient(_serverUrl, httpClient);
        var common = CreateCommon(version: "2.0.0", platform: "Linux", environment: Environment.Release);

        var exception = new ArgumentNullException("testParam", "Test parameter cannot be null");

        // Act
        var reportId = await client.SendCrashAsync(common, exception);

        // Assert
        reportId.Should().NotBeNull();

        await using var dbContext = _dbFixture.CreateDbContext();
        var savedError = await dbContext.Errors
            .Where(e => e.Version == "2.0.0" && e.Platform == "Linux")
            .FirstOrDefaultAsync();

        savedError.Should().NotBeNull();
        savedError!.Message.Should().Contain("Test parameter cannot be null");
        savedError.StackTrace.Should().NotBeEmpty();
        savedError.Log.Should().NotBeEmpty();
    }


    [Fact]
    public async Task SendCrashAsync_SameExceptionTwice_CreatesNewRecords()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        using var client = new DomovoyClient(_serverUrl, httpClient);
        var common = CreateCommon();

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
        var reportId1 = await client.SendCrashAsync(common, capturedException!);
        var reportId2 = await client.SendCrashAsync(common, capturedException!);

        // Assert - Both should succeed
        reportId1.Should().NotBeNull();
        reportId2.Should().NotBeNull();

        // Verify two separate records were created
        await using var dbContext = _dbFixture.CreateDbContext();
        var errorCount = await dbContext.Errors.CountAsync();

        errorCount.Should().Be(2);
    }

    [Fact]
    public void Constructor_WithNullServerUrl_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new DomovoyClient(null!);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("serverUrl");
    }

    [Fact]
    public void Constructor_WithEmptyServerUrl_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new DomovoyClient("");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("serverUrl");
    }

    [Fact]
    public async Task SendCrashAsync_WithNullStandardPayload_ThrowsArgumentNullException()
    {
        // Arrange
        using var client = new DomovoyClient(_serverUrl);
        var exception = new InvalidOperationException("Test");

        // Act & Assert
        var act = async () => await client.SendCrashAsync(null!, exception);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("common");
    }

    [Fact]
    public async Task SendCrashAsync_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        using var client = new DomovoyClient(_serverUrl);
        var common = CreateCommon();

        // Act & Assert
        var act = async () => await client.SendCrashAsync(common, null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("exception");
    }

    [Fact]
    public async Task SendEventAsync_WithValidData_ReturnsReportId()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        using var client = new DomovoyClient(_serverUrl, httpClient);
        var common = CreateCommon();

        // Act
        var reportId = await client.SendEventAsync(common, "Testing", "TestEvent");

        // Assert
        reportId.Should().NotBeNull();
        reportId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task SendErrorAsync_WithValidData_ReturnsReportId()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        using var client = new DomovoyClient(_serverUrl, httpClient);
        var common = CreateCommon();

        // Act
        var reportId = await client.SendErrorAsync(
            common,
            Severity.Error,
            "Test error",
            "at TestMethod() in Test.cs:line 1",
            "Test error log");

        // Assert
        reportId.Should().NotBeNull();
        reportId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task SendErrorAsync_WithInlineAttachments_UploadsAttachments()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        using var client = new DomovoyClient(_serverUrl, httpClient);
        var common = CreateCommon();

        var attachments = new List<FileAttachment>
        {
            new() { Stream = new MemoryStream(Encoding.UTF8.GetBytes("screenshot data")), Filename = "screenshot.png", ContentType = "image/png" },
            new() { Stream = new MemoryStream(Encoding.UTF8.GetBytes("log file data")), Filename = "game.log" }
        };

        // Act
        var reportId = await client.SendErrorAsync(
            common,
            Severity.Error,
            "Error with inline attachments",
            "at TestMethod() in Test.cs:line 1",
            "Test log",
            attachments);

        // Assert
        reportId.Should().NotBeNull();
        reportId.Should().NotBe(Guid.Empty);

        await using var dbContext = _dbFixture.CreateDbContext();
        var savedAttachments = await dbContext.Attachments
            .Where(a => a.ReportId == reportId!.Value)
            .OrderBy(a => a.Filename)
            .ToListAsync();

        savedAttachments.Should().HaveCount(2);
        savedAttachments[0].Filename.Should().Be("game.log");
        savedAttachments[1].Filename.Should().Be("screenshot.png");
        savedAttachments[1].ContentType.Should().Be("image/png");
    }
}
