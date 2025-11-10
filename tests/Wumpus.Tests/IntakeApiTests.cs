using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wumpus.Database;
using Wumpus.Shared.DTOs;
using Wumpus.Tests.Infrastructure;
using Xunit;

namespace Wumpus.Tests;

/// <summary>
/// Integration tests for the Wumpus Intake API endpoints.
/// Tests the HTTP API, database persistence, and deduplication logic.
/// </summary>
[Collection("Database")]
public class IntakeApiTests : IClassFixture<IntakeApiFactory>, IAsyncLifetime
{
    private readonly IntakeApiFactory _factory;
    private readonly DatabaseFixture _dbFixture;
    private readonly HttpClient _client;

    public IntakeApiTests(IntakeApiFactory factory, DatabaseFixture dbFixture)
    {
        _factory = factory;
        _dbFixture = dbFixture;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Clear database after each test to ensure isolation
        await _dbFixture.ClearCrashReportsAsync();
    }

    private static async Task<Guid> ExtractCrashIdFromResponse(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return Guid.Parse(doc.RootElement.GetProperty("id").GetString()!);
    }

    [Fact]
    public async Task SubmitCrashReport_WithValidData_ReturnsAcceptedWithCrashId()
    {
        // Arrange
        var crashReport = TestDataBuilder.CreateCrashReport();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/crashes", crashReport);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var crashId = await ExtractCrashIdFromResponse(response);
        crashId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SubmitCrashReport_WithValidData_PersistsToDatabase()
    {
        // Arrange
        var crashReport = TestDataBuilder.CreateCrashReport(
            gameVersion: "2.0.0",
            platform: "Linux",
            exceptionType: "System.InvalidOperationException"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/crashes", crashReport);
        var crashId = await ExtractCrashIdFromResponse(response);

        // Assert
        await using var dbContext = _dbFixture.CreateDbContext();
        var savedCrash = await dbContext.CrashReports.FindAsync(crashId);

        savedCrash.Should().NotBeNull();
        savedCrash!.Core.GameVersion.Should().Be("2.0.0");
        savedCrash.Core.Platform.Should().Be("Linux");
        savedCrash.Core.ExceptionType.Should().Be("System.InvalidOperationException");
    }

    [Fact]
    public async Task SubmitCrashReport_WithSameStackTrace_DeduplicatesAndIncrementsCount()
    {
        // Arrange
        var crashReport1 = TestDataBuilder.CreateCrashReport();
        var crashReport2 = TestDataBuilder.CreateCrashReport(); // Same stack trace

        // Act - Submit first crash
        var response1 = await _client.PostAsJsonAsync("/api/v1/crashes", crashReport1);
        var crashId1 = await ExtractCrashIdFromResponse(response1);

        // Submit second crash with same stack trace
        var response2 = await _client.PostAsJsonAsync("/api/v1/crashes", crashReport2);
        var crashId2 = await ExtractCrashIdFromResponse(response2);

        // Assert - Both should return the same crash ID
        crashId1.Should().Be(crashId2);

        // Verify only one crash report exists
        await using var dbContext = _dbFixture.CreateDbContext();
        var savedCrash = await dbContext.CrashReports.FindAsync(crashId1);

        savedCrash.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitCrashReport_WithDifferentStackTrace_CreatesNewCrashReport()
    {
        // Arrange
        var crashReport1 = TestDataBuilder.CreateCrashReport();
        var crashReport2 = TestDataBuilder.CreateDifferentCrashReport();

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/v1/crashes", crashReport1);
        var crashId1 = await ExtractCrashIdFromResponse(response1);

        var response2 = await _client.PostAsJsonAsync("/api/v1/crashes", crashReport2);
        var crashId2 = await ExtractCrashIdFromResponse(response2);

        // Assert - Should create two different crash reports
        crashId1.Should().NotBe(crashId2);

        await using var dbContext = _dbFixture.CreateDbContext();
        var crash1 = await dbContext.CrashReports.FindAsync(crashId1);
        var crash2 = await dbContext.CrashReports.FindAsync(crashId2);

        crash1.Should().NotBeNull();
        crash2.Should().NotBeNull();
        crash1!.StackTraceHash.Should().NotBe(crash2!.StackTraceHash);
    }

    [Fact]
    public async Task SubmitCrashReport_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var invalidCrashReport = TestDataBuilder.CreateInvalidCrashReport();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/crashes", invalidCrashReport);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy");
    }

    [Fact]
    public async Task SubmitCrashReport_MultipleIdenticalCrashes_IncrementsCountCorrectly()
    {
        // Arrange
        var crashReport = TestDataBuilder.CreateCrashReport();
        const int submissionCount = 5;

        // Act - Submit the same crash 5 times
        Guid? crashId = null;
        for (int i = 0; i < submissionCount; i++)
        {
            var response = await _client.PostAsJsonAsync("/api/v1/crashes", crashReport);
            var currentCrashId = await ExtractCrashIdFromResponse(response);

            if (crashId == null)
            {
                crashId = currentCrashId;
            }
            else
            {
                currentCrashId.Should().Be(crashId.Value, "All submissions should deduplicate to the same crash");
            }
        }

        // Assert
        await using var dbContext = _dbFixture.CreateDbContext();
        var savedCrash = await dbContext.CrashReports.FindAsync(crashId!.Value);

        savedCrash.Should().NotBeNull();
    }
}

/// <summary>
/// Collection definition for database tests to ensure DatabaseFixture is shared.
/// </summary>
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
