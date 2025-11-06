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
/// Tests the core deduplication logic, hashing algorithm, and database operations.
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
        savedCrash!.OccurrenceCount.Should().Be(1);
        savedCrash.Core.GameVersion.Should().Be("1.0.0");
        savedCrash.Core.Platform.Should().Be("Windows");
        savedCrash.FirstSeen.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        savedCrash.LastSeen.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ProcessCrashReportAsync_DuplicateCrash_UpdatesExistingRecord()
    {
        // Arrange
        var request1 = TestDataBuilder.CreateCrashReport();
        var request2 = TestDataBuilder.CreateCrashReport(); // Same crash

        // Act - Process first crash
        var crashId1 = await _service.ProcessCrashReportAsync(request1);
        var firstSeen = (await _dbContext.CrashReports.FindAsync(crashId1))!.FirstSeen;

        // Wait to ensure different LastSeen timestamp
        await Task.Delay(100);

        // Process duplicate crash
        var crashId2 = await _service.ProcessCrashReportAsync(request2);

        // Assert - Should return the same crash ID
        crashId2.Should().Be(crashId1);

        var savedCrash = await _dbContext.CrashReports.FindAsync(crashId1);
        savedCrash.Should().NotBeNull();
        savedCrash!.OccurrenceCount.Should().Be(2);
        savedCrash.FirstSeen.Should().Be(firstSeen, "FirstSeen should not change");
        savedCrash.LastSeen.Should().BeAfter(firstSeen, "LastSeen should be updated");
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
        crash1!.StackTraceHash.Should().NotBe(crash2!.StackTraceHash);
    }

    [Fact]
    public async Task ProcessCrashReportAsync_StackTraceHashing_UsesFirst5Frames()
    {
        // Arrange - Two stack traces with same first 5 frames but different later frames
        var stackTrace1 = @"   at Game.Player.PlayerController.Move(Vector3 direction) in C:\Game\Player\PlayerController.cs:line 42
   at Game.Player.PlayerInputHandler.HandleInput() in C:\Game\Player\PlayerInputHandler.cs:line 78
   at Game.Core.GameLoop.Update() in C:\Game\Core\GameLoop.cs:line 120
   at Game.Core.GameLoop.Run() in C:\Game\Core\GameLoop.cs:line 55
   at Game.Program.Main(String[] args) in C:\Game\Program.cs:line 18
   at System.AppDomain.ExecuteAssembly(String assemblyFile)
   at Microsoft.VisualStudio.HostingProcess.HostProc.RunUsersAssembly()";

        var stackTrace2 = @"   at Game.Player.PlayerController.Move(Vector3 direction) in C:\Game\Player\PlayerController.cs:line 42
   at Game.Player.PlayerInputHandler.HandleInput() in C:\Game\Player\PlayerInputHandler.cs:line 78
   at Game.Core.GameLoop.Update() in C:\Game\Core\GameLoop.cs:line 120
   at Game.Core.GameLoop.Run() in C:\Game\Core\GameLoop.cs:line 55
   at Game.Program.Main(String[] args) in C:\Game\Program.cs:line 18
   at DifferentNamespace.DifferentMethod()
   at AnotherNamespace.AnotherMethod()";

        var request1 = TestDataBuilder.CreateCrashReportWithStackTrace(stackTrace1);
        var request2 = TestDataBuilder.CreateCrashReportWithStackTrace(stackTrace2);

        // Act
        var crashId1 = await _service.ProcessCrashReportAsync(request1);
        var crashId2 = await _service.ProcessCrashReportAsync(request2);

        // Assert - Should deduplicate because first 5 frames are identical
        crashId1.Should().Be(crashId2, "Crashes with same first 5 frames should deduplicate");

        var savedCrash = await _dbContext.CrashReports.FindAsync(crashId1);
        savedCrash.Should().NotBeNull();
        savedCrash!.OccurrenceCount.Should().Be(2);
    }

    [Fact]
    public async Task ProcessCrashReportAsync_DifferentFirst5Frames_CreatesSeparateRecords()
    {
        // Arrange - Stack traces with different first frame
        var stackTrace1 = @"   at Game.Player.PlayerController.Move(Vector3 direction) in C:\Game\Player\PlayerController.cs:line 42
   at Game.Player.PlayerInputHandler.HandleInput() in C:\Game\Player\PlayerInputHandler.cs:line 78";

        var stackTrace2 = @"   at Game.Enemy.EnemyController.Attack(Player target) in C:\Game\Enemy\EnemyController.cs:line 100
   at Game.Player.PlayerInputHandler.HandleInput() in C:\Game\Player\PlayerInputHandler.cs:line 78";

        var request1 = TestDataBuilder.CreateCrashReportWithStackTrace(stackTrace1);
        var request2 = TestDataBuilder.CreateCrashReportWithStackTrace(stackTrace2);

        // Act
        var crashId1 = await _service.ProcessCrashReportAsync(request1);
        var crashId2 = await _service.ProcessCrashReportAsync(request2);

        // Assert - Should create separate records
        crashId1.Should().NotBe(crashId2);

        var crash1 = await _dbContext.CrashReports.FindAsync(crashId1);
        var crash2 = await _dbContext.CrashReports.FindAsync(crashId2);

        crash1!.StackTraceHash.Should().NotBe(crash2!.StackTraceHash);
    }

    [Fact]
    public async Task ProcessCrashReportAsync_MultipleOccurrences_IncrementsCountCorrectly()
    {
        // Arrange
        var request = TestDataBuilder.CreateCrashReport();
        const int occurrenceCount = 10;

        // Act - Submit the same crash multiple times
        Guid? crashId = null;
        for (int i = 0; i < occurrenceCount; i++)
        {
            var id = await _service.ProcessCrashReportAsync(request);
            if (crashId == null)
            {
                crashId = id;
            }
            else
            {
                id.Should().Be(crashId.Value);
            }

            await Task.Delay(10); // Small delay for timestamp variation
        }

        // Assert
        var savedCrash = await _dbContext.CrashReports.FindAsync(crashId!.Value);
        savedCrash.Should().NotBeNull();
        savedCrash!.OccurrenceCount.Should().Be(occurrenceCount);
    }

    [Fact]
    public async Task ProcessCrashReportAsync_WithSystemInfo_StoresAsJson()
    {
        // Arrange
        var systemInfo = new Dictionary<string, string>
        {
            { "OS", "Ubuntu 22.04" },
            { "CPU", "AMD Ryzen 9 5900X" },
            { "RAM", "32 GB" }
        };
        var request = TestDataBuilder.CreateCrashReport(systemInfo: systemInfo);

        // Act
        var crashId = await _service.ProcessCrashReportAsync(request);

        // Assert
        var savedCrash = await _dbContext.CrashReports.FindAsync(crashId);
        savedCrash.Should().NotBeNull();
        savedCrash!.SystemInfo.Should().NotBeNullOrEmpty();
        savedCrash.SystemInfo.Should().Contain("Ubuntu 22.04");
        savedCrash.SystemInfo.Should().Contain("AMD Ryzen 9 5900X");
    }

    [Fact]
    public async Task ProcessCrashReportAsync_WithUserContext_StoresAsJson()
    {
        // Arrange
        var userContext = new Dictionary<string, string>
        {
            { "UserId", "test_user_123" },
            { "Level", "Boss_Fight_3" },
            { "PlayTime", "247.5" }
        };
        var request = TestDataBuilder.CreateCrashReport(userContext: userContext);

        // Act
        var crashId = await _service.ProcessCrashReportAsync(request);

        // Assert
        var savedCrash = await _dbContext.CrashReports.FindAsync(crashId);
        savedCrash.Should().NotBeNull();
        savedCrash!.UserContext.Should().NotBeNullOrEmpty();
        savedCrash.UserContext.Should().Contain("test_user_123");
        savedCrash.UserContext.Should().Contain("Boss_Fight_3");
    }
}
