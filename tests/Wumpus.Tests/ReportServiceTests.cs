using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Wumpus.Database;
using Wumpus.Intake.Services;
using Wumpus.Shared.Models;
using Wumpus.Tests.Infrastructure;
using Xunit;

namespace Wumpus.Tests;

/// <summary>
/// Integration tests for the ReportService.
/// Tests error report creation and database operations.
/// </summary>
[Collection("Database")]
public class ReportServiceTests : IAsyncLifetime
{
    private readonly DatabaseFixture _dbFixture;
    private readonly WumpusDbContext _dbContext;
    private readonly ReportService _service;

    public ReportServiceTests(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _dbContext = _dbFixture.CreateDbContext();
        _service = new ReportService(_dbContext, NullLogger<ReportService>.Instance);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _dbFixture.ClearReportsAsync();
    }

    [Fact]
    public async Task ProcessErrorAsync_NewError_CreatesNewRecord()
    {
        // Arrange
        var request = TestDataBuilder.CreateErrorReport();

        // Act
        var errorId = await _service.ProcessErrorAsync(request);

        // Assert
        errorId.Should().NotBeEmpty();

        var savedError = await _dbContext.Errors.FindAsync(errorId);
        savedError.Should().NotBeNull();
        savedError!.GameVersion.Should().Be("1.0.0");
        savedError.Platform.Should().Be("Windows");
        savedError.Data.Severity.Should().Be(Severity.Fatal);
    }

    [Fact]
    public async Task ProcessErrorAsync_DuplicateError_CreatesNewRecord()
    {
        // Arrange
        var request1 = TestDataBuilder.CreateErrorReport();
        var request2 = TestDataBuilder.CreateErrorReport(); // Same error

        // Act - Process first error
        var errorId1 = await _service.ProcessErrorAsync(request1);

        // Process duplicate error
        var errorId2 = await _service.ProcessErrorAsync(request2);

        // Assert - Should create separate records even with identical errors
        errorId2.Should().NotBe(errorId1);

        var savedError1 = await _dbContext.Errors.FindAsync(errorId1);
        var savedError2 = await _dbContext.Errors.FindAsync(errorId2);

        savedError1.Should().NotBeNull();
        savedError2.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessErrorAsync_DifferentStackTrace_CreatesNewRecord()
    {
        // Arrange
        var request1 = TestDataBuilder.CreateErrorReport();
        var request2 = TestDataBuilder.CreateDifferentErrorReport();

        // Act
        var errorId1 = await _service.ProcessErrorAsync(request1);
        var errorId2 = await _service.ProcessErrorAsync(request2);

        // Assert - Should create two separate records
        errorId1.Should().NotBe(errorId2);

        var error1 = await _dbContext.Errors.FindAsync(errorId1);
        var error2 = await _dbContext.Errors.FindAsync(errorId2);

        error1.Should().NotBeNull();
        error2.Should().NotBeNull();
    }
}
