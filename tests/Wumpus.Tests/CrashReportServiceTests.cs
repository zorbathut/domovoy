using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Wumpus.Database;
using Wumpus.Intake.Services;
using Wumpus.Tests.Infrastructure;
using Xunit;

namespace Wumpus.Tests;

/// <summary>
/// Integration tests for the CrashReportService.
/// Tests crash report creation and database operations.
/// </summary>
[Collection("Database")]
public class CrashReportServiceTests : IAsyncLifetime
{
    private readonly DatabaseFixture _dbFixture;
    private readonly WumpusDbContext _dbContext;
    private readonly CrashReportService _service;

    public CrashReportServiceTests(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _dbContext = _dbFixture.CreateDbContext();
        _service = new CrashReportService(_dbContext, NullLogger<CrashReportService>.Instance);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _dbFixture.ClearCrashReportsAsync();
    }

    [Fact]
    public async Task ProcessCrashReportAsync_NewCrash_CreatesNewRecord()
    {
        // Arrange
        var request = TestDataBuilder.CreateCrashReport();

        // Act
        var crashId = await _service.ProcessCrashReportAsync(request);

        // Assert
        crashId.Should().NotBeEmpty();

        var savedCrash = await _dbContext.CrashReports.FindAsync(crashId);
        savedCrash.Should().NotBeNull();
        savedCrash!.Core.GameVersion.Should().Be("1.0.0");
        savedCrash.Core.Platform.Should().Be("Windows");
    }

    [Fact]
    public async Task ProcessCrashReportAsync_DuplicateCrash_CreatesNewRecord()
    {
        // Arrange
        var request1 = TestDataBuilder.CreateCrashReport();
        var request2 = TestDataBuilder.CreateCrashReport(); // Same crash

        // Act - Process first crash
        var crashId1 = await _service.ProcessCrashReportAsync(request1);

        // Process duplicate crash
        var crashId2 = await _service.ProcessCrashReportAsync(request2);

        // Assert - Should create separate records even with identical crashes
        crashId2.Should().NotBe(crashId1);

        var savedCrash1 = await _dbContext.CrashReports.FindAsync(crashId1);
        var savedCrash2 = await _dbContext.CrashReports.FindAsync(crashId2);

        savedCrash1.Should().NotBeNull();
        savedCrash2.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessCrashReportAsync_DifferentStackTrace_CreatesNewRecord()
    {
        // Arrange
        var request1 = TestDataBuilder.CreateCrashReport();
        var request2 = TestDataBuilder.CreateDifferentCrashReport();

        // Act
        var crashId1 = await _service.ProcessCrashReportAsync(request1);
        var crashId2 = await _service.ProcessCrashReportAsync(request2);

        // Assert - Should create two separate records
        crashId1.Should().NotBe(crashId2);

        var crash1 = await _dbContext.CrashReports.FindAsync(crashId1);
        var crash2 = await _dbContext.CrashReports.FindAsync(crashId2);

        crash1.Should().NotBeNull();
        crash2.Should().NotBeNull();
    }
}
