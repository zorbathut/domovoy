using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public async Task SubmitErrorReport_WithValidData_ReturnsAccepted()
    {
        // Arrange
        var errorReport = TestDataBuilder.CreateErrorReport();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/reports/error", errorReport);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
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

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        await using var dbContext = _dbFixture.CreateDbContext();
        var savedError = await dbContext.Errors
            .Where(e => e.GameVersion == "2.0.0" && e.Platform == "Linux")
            .FirstOrDefaultAsync();

        savedError.Should().NotBeNull();
        savedError!.Data.ExceptionType.Should().Be("System.InvalidOperationException");
    }

    [Fact]
    public async Task SubmitErrorReport_WithSameStackTrace_CreatesNewRecords()
    {
        // Arrange
        var errorReport1 = TestDataBuilder.CreateErrorReport();
        var errorReport2 = TestDataBuilder.CreateErrorReport(); // Same stack trace

        // Act - Submit first error
        var response1 = await _client.PostAsJsonAsync("/api/v1/reports/error", errorReport1);
        response1.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Submit second error with same stack trace
        var response2 = await _client.PostAsJsonAsync("/api/v1/reports/error", errorReport2);
        response2.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Assert - Should create separate records even with identical stack traces
        await using var dbContext = _dbFixture.CreateDbContext();
        var errorCount = await dbContext.Errors.CountAsync();

        errorCount.Should().Be(2);
    }

    [Fact]
    public async Task SubmitErrorReport_WithDifferentStackTrace_CreatesNewErrorReport()
    {
        // Arrange
        var errorReport1 = TestDataBuilder.CreateErrorReport();
        var errorReport2 = TestDataBuilder.CreateDifferentErrorReport();

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/v1/reports/error", errorReport1);
        response1.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var response2 = await _client.PostAsJsonAsync("/api/v1/reports/error", errorReport2);
        response2.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Assert - Should create two different error reports
        await using var dbContext = _dbFixture.CreateDbContext();
        var errorCount = await dbContext.Errors.CountAsync();

        errorCount.Should().Be(2);
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
        for (int i = 0; i < submissionCount; i++)
        {
            var response = await _client.PostAsJsonAsync("/api/v1/reports/error", errorReport);
            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        // Assert - Should have created 5 separate records
        await using var dbContext = _dbFixture.CreateDbContext();
        var errorCount = await dbContext.Errors.CountAsync();

        errorCount.Should().Be(submissionCount);
    }
}

/// <summary>
/// Collection definition for database tests to ensure DatabaseFixture is shared.
/// </summary>
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
