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
/// Tests the HTTP API and database persistence.
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
        await _dbFixture.ClearReportsAsync();
    }

    private static async Task<Guid> ExtractIdFromResponse(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return Guid.Parse(doc.RootElement.GetProperty("id").GetString()!);
    }

    [Fact]
    public async Task SubmitErrorReport_WithValidData_ReturnsAcceptedWithErrorId()
    {
        // Arrange
        var errorReport = TestDataBuilder.CreateErrorReport();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/reports/error", errorReport);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var errorId = await ExtractIdFromResponse(response);
        errorId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SubmitErrorReport_WithValidData_PersistsToDatabase()
    {
        // Arrange
        var errorReport = TestDataBuilder.CreateErrorReport(
            gameVersion: "2.0.0",
            platform: "Linux",
            exceptionType: "System.InvalidOperationException"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/reports/error", errorReport);
        var errorId = await ExtractIdFromResponse(response);

        // Assert
        await using var dbContext = _dbFixture.CreateDbContext();
        var savedError = await dbContext.Errors.FindAsync(errorId);

        savedError.Should().NotBeNull();
        savedError!.GameVersion.Should().Be("2.0.0");
        savedError.Platform.Should().Be("Linux");
        savedError.Data.ExceptionType.Should().Be("System.InvalidOperationException");
    }

    [Fact]
    public async Task SubmitErrorReport_WithSameStackTrace_CreatesNewRecords()
    {
        // Arrange
        var errorReport1 = TestDataBuilder.CreateErrorReport();
        var errorReport2 = TestDataBuilder.CreateErrorReport(); // Same stack trace

        // Act - Submit first error
        var response1 = await _client.PostAsJsonAsync("/api/v1/reports/error", errorReport1);
        var errorId1 = await ExtractIdFromResponse(response1);

        // Submit second error with same stack trace
        var response2 = await _client.PostAsJsonAsync("/api/v1/reports/error", errorReport2);
        var errorId2 = await ExtractIdFromResponse(response2);

        // Assert - Should create separate records even with identical stack traces
        errorId1.Should().NotBe(errorId2);

        // Verify two error reports exist
        await using var dbContext = _dbFixture.CreateDbContext();
        var savedError1 = await dbContext.Errors.FindAsync(errorId1);
        var savedError2 = await dbContext.Errors.FindAsync(errorId2);

        savedError1.Should().NotBeNull();
        savedError2.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitErrorReport_WithDifferentStackTrace_CreatesNewErrorReport()
    {
        // Arrange
        var errorReport1 = TestDataBuilder.CreateErrorReport();
        var errorReport2 = TestDataBuilder.CreateDifferentErrorReport();

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/v1/reports/error", errorReport1);
        var errorId1 = await ExtractIdFromResponse(response1);

        var response2 = await _client.PostAsJsonAsync("/api/v1/reports/error", errorReport2);
        var errorId2 = await ExtractIdFromResponse(response2);

        // Assert - Should create two different error reports
        errorId1.Should().NotBe(errorId2);

        await using var dbContext = _dbFixture.CreateDbContext();
        var error1 = await dbContext.Errors.FindAsync(errorId1);
        var error2 = await dbContext.Errors.FindAsync(errorId2);

        error1.Should().NotBeNull();
        error2.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitErrorReport_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var invalidErrorReport = TestDataBuilder.CreateInvalidErrorReport();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/reports/error", invalidErrorReport);

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
    public async Task SubmitErrorReport_MultipleIdenticalErrors_CreatesMultipleRecords()
    {
        // Arrange
        var errorReport = TestDataBuilder.CreateErrorReport();
        const int submissionCount = 5;

        // Act - Submit the same error 5 times
        var errorIds = new List<Guid>();
        for (int i = 0; i < submissionCount; i++)
        {
            var response = await _client.PostAsJsonAsync("/api/v1/reports/error", errorReport);
            var currentErrorId = await ExtractIdFromResponse(response);
            errorIds.Add(currentErrorId);
        }

        // Assert - All IDs should be unique
        errorIds.Should().OnlyHaveUniqueItems("Each submission should create a new error report");
        errorIds.Should().HaveCount(submissionCount);

        // Verify all error reports exist in database
        await using var dbContext = _dbFixture.CreateDbContext();
        foreach (var errorId in errorIds)
        {
            var savedError = await dbContext.Errors.FindAsync(errorId);
            savedError.Should().NotBeNull();
        }
    }
}

/// <summary>
/// Collection definition for database tests to ensure DatabaseFixture is shared.
/// </summary>
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
