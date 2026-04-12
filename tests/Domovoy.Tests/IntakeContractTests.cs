using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Domovoy.Tests.Infrastructure;
using Xunit;

namespace Domovoy.Tests;

/// <summary>
/// FROZEN CONTRACT TESTS for the Intake API.
///
/// These tests send raw, hand-frozen JSON payloads to the /api/v1/reports/event
/// and /api/v1/reports/error endpoints and verify both HTTP acceptance and
/// correct database persistence.
///
/// PURPOSE: deployed game clients send these exact JSON shapes. If a change to
/// the server breaks any of these tests, it is a BREAKING WIRE FORMAT CHANGE
/// that will break shipped games. Bump the API version (/api/v2/...) and add
/// a new endpoint instead — DO NOT modify the existing fixtures.
///
/// To add a new fixture: see ContractCaptureUtility for instructions on
/// generating the JSON from a typed request object.
/// </summary>
[Collection("Database")]
public class IntakeContractTests : IClassFixture<IntakeApiFactory>, IAsyncLifetime
{
    private readonly IntakeApiFactory _factory;
    private readonly DatabaseFixture _dbFixture;
    private readonly HttpClient _client;

    public IntakeContractTests(IntakeApiFactory factory, DatabaseFixture dbFixture)
    {
        _factory = factory;
        _dbFixture = dbFixture;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _dbFixture.ClearReportsAsync();
    }

    // ============================================================================
    // FROZEN FIXTURES — these JSON strings represent the wire format of deployed
    // game clients. NEVER MODIFY THE CONTENTS of these constants. If a new wire
    // format is needed, add a NEW constant and a NEW endpoint version (/api/v2/...).
    // ============================================================================
    private static class FrozenFixtures
    {
        // ============================================================
        // FROZEN CONTRACT — DO NOT MODIFY THIS JSON
        // Minimal event payload: only required fields populated.
        // Optional Guid fields default to all-zeros, campaignSequenceIds is
        // an empty array, and metadata/data are omitted entirely.
        // ============================================================
        public const string MinimalEventJson = """
            {
              "version": "1.0.0",
              "platform": "Windows",
              "environment": "Dev",
              "userId": "00000000-0000-0000-0000-000000000000",
              "computerId": "00000000-0000-0000-0000-000000000000",
              "campaignId": "00000000-0000-0000-0000-000000000000",
              "campaignSequenceIds": [],
              "processId": "00000000-0000-0000-0000-000000000000",
              "category": "MinimalCategory",
              "name": "MinimalEvent"
            }
            """;

        // ============================================================
        // FROZEN CONTRACT — DO NOT MODIFY THIS JSON
        // Comprehensive event payload: every field populated, including
        // metadata, data, and a multi-entry campaignSequenceIds list.
        // ============================================================
        public const string ComprehensiveEventJson = """
            {
              "version": "2.5.1",
              "platform": "Linux",
              "environment": "Production",
              "userId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
              "computerId": "b2c3d4e5-f6a7-4890-bcde-f12345678901",
              "campaignId": "c3d4e5f6-a7b8-4901-cdef-123456789012",
              "campaignSequenceIds": [
                "d4e5f6a7-b8c9-4012-defa-234567890123",
                "e5f6a7b8-c9d0-4123-efab-345678901234"
              ],
              "processId": "f6a7b8c9-d0e1-4234-fabc-456789012345",
              "metadata": {
                "buildNumber": 4321,
                "branch": "main",
                "ciRun": true
              },
              "category": "Gameplay",
              "name": "LevelCompleted",
              "data": {
                "level": 5,
                "timeSeconds": 120.5,
                "playerName": "Zorba"
              }
            }
            """;

        // ============================================================
        // FROZEN CONTRACT — DO NOT MODIFY THIS JSON
        // Minimal error payload: only required fields populated.
        // severity = 3 corresponds to Severity.Error (the DTO default).
        // ============================================================
        public const string MinimalErrorJson = """
            {
              "version": "1.0.0",
              "platform": "Windows",
              "environment": "Dev",
              "userId": "00000000-0000-0000-0000-000000000000",
              "computerId": "00000000-0000-0000-0000-000000000000",
              "campaignId": "00000000-0000-0000-0000-000000000000",
              "campaignSequenceIds": [],
              "processId": "00000000-0000-0000-0000-000000000000",
              "severity": 3,
              "message": "Minimal error message",
              "stackTrace": "   at Game.Foo() in Foo.cs:line 1",
              "log": "Minimal error log"
            }
            """;

        // ============================================================
        // FROZEN CONTRACT — DO NOT MODIFY THIS JSON
        // Comprehensive error payload: every field populated.
        // severity = 4 corresponds to Severity.Fatal.
        // ============================================================
        public const string ComprehensiveErrorJson = """
            {
              "version": "2.5.1",
              "platform": "Linux",
              "environment": "Production",
              "userId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
              "computerId": "b2c3d4e5-f6a7-4890-bcde-f12345678901",
              "campaignId": "c3d4e5f6-a7b8-4901-cdef-123456789012",
              "campaignSequenceIds": [
                "d4e5f6a7-b8c9-4012-defa-234567890123"
              ],
              "processId": "f6a7b8c9-d0e1-4234-fabc-456789012345",
              "metadata": {
                "buildNumber": 4321,
                "branch": "main"
              },
              "severity": 4,
              "message": "Object reference not set to an instance of an object.",
              "stackTrace": "   at Game.Player.PlayerController.Move(Vector3 direction) in C:\\Game\\Player\\PlayerController.cs:line 42\n   at Game.Core.GameLoop.Update() in C:\\Game\\Core\\GameLoop.cs:line 120",
              "log": "System.NullReferenceException: Object reference not set to an instance of an object."
            }
            """;
    }

    // ----------------------------------------------------------------------------
    // Tests
    // ----------------------------------------------------------------------------

    [Fact]
    public async Task FrozenContract_MinimalEvent_AcceptedAndPersisted()
    {
        var reportId = await PostAndExpectAcceptedAsync("/api/v1/reports/event", FrozenFixtures.MinimalEventJson);

        await using var db = _dbFixture.CreateDbContext();
        var saved = await db.Events.SingleAsync(e => e.Id == reportId);

        saved.Version.Should().Be("1.0.0");
        saved.Platform.Should().Be("Windows");
        saved.Environment.Should().Be("Dev");
        saved.Category.Should().Be("MinimalCategory");
        saved.Name.Should().Be("MinimalEvent");
        saved.UserId.Should().Be(Guid.Empty);
        saved.ComputerId.Should().Be(Guid.Empty);
        saved.CampaignId.Should().Be(Guid.Empty);
        saved.ProcessId.Should().Be(Guid.Empty);
        saved.CampaignSequenceIds.Should().BeEmpty();
        saved.Metadata.Should().BeNull();
        saved.Data.Should().BeNull();
    }

    [Fact]
    public async Task FrozenContract_ComprehensiveEvent_AcceptedAndPersisted()
    {
        var reportId = await PostAndExpectAcceptedAsync("/api/v1/reports/event", FrozenFixtures.ComprehensiveEventJson);

        await using var db = _dbFixture.CreateDbContext();
        var saved = await db.Events.SingleAsync(e => e.Id == reportId);

        saved.Version.Should().Be("2.5.1");
        saved.Platform.Should().Be("Linux");
        saved.Environment.Should().Be("Production");
        saved.Category.Should().Be("Gameplay");
        saved.Name.Should().Be("LevelCompleted");
        saved.UserId.Should().Be(Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"));
        saved.ComputerId.Should().Be(Guid.Parse("b2c3d4e5-f6a7-4890-bcde-f12345678901"));
        saved.CampaignId.Should().Be(Guid.Parse("c3d4e5f6-a7b8-4901-cdef-123456789012"));
        saved.ProcessId.Should().Be(Guid.Parse("f6a7b8c9-d0e1-4234-fabc-456789012345"));
        saved.CampaignSequenceIds.Should().HaveCount(2);
        saved.CampaignSequenceIds[0].Should().Be(Guid.Parse("d4e5f6a7-b8c9-4012-defa-234567890123"));
        saved.CampaignSequenceIds[1].Should().Be(Guid.Parse("e5f6a7b8-c9d0-4123-efab-345678901234"));
        saved.Metadata.Should().NotBeNull();
        saved.Metadata!.Should().ContainKey("branch");
        saved.Data.Should().NotBeNull();
        saved.Data!.Should().ContainKey("playerName");
    }

    [Fact]
    public async Task FrozenContract_MinimalError_AcceptedAndPersisted()
    {
        var reportId = await PostAndExpectAcceptedAsync("/api/v1/reports/error", FrozenFixtures.MinimalErrorJson);

        await using var db = _dbFixture.CreateDbContext();
        var saved = await db.Errors.SingleAsync(e => e.Id == reportId);

        saved.Version.Should().Be("1.0.0");
        saved.Platform.Should().Be("Windows");
        saved.Environment.Should().Be("Dev");
        saved.Severity.Should().Be(Domovoy.Shared.Models.Severity.Error);
        saved.Message.Should().Be("Minimal error message");
        saved.StackTrace.Should().Be("   at Game.Foo() in Foo.cs:line 1");
        saved.Log.Should().Be("Minimal error log");
        saved.UserId.Should().Be(Guid.Empty);
        saved.CampaignSequenceIds.Should().BeEmpty();
        saved.Metadata.Should().BeNull();
    }

    [Fact]
    public async Task FrozenContract_ComprehensiveError_AcceptedAndPersisted()
    {
        var reportId = await PostAndExpectAcceptedAsync("/api/v1/reports/error", FrozenFixtures.ComprehensiveErrorJson);

        await using var db = _dbFixture.CreateDbContext();
        var saved = await db.Errors.SingleAsync(e => e.Id == reportId);

        saved.Version.Should().Be("2.5.1");
        saved.Platform.Should().Be("Linux");
        saved.Environment.Should().Be("Production");
        saved.Severity.Should().Be(Domovoy.Shared.Models.Severity.Fatal);
        saved.Message.Should().Be("Object reference not set to an instance of an object.");
        saved.StackTrace.Should().Contain("PlayerController.Move");
        saved.Log.Should().StartWith("System.NullReferenceException");
        saved.UserId.Should().Be(Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"));
        saved.CampaignSequenceIds.Should().HaveCount(1);
        saved.Metadata.Should().NotBeNull();
        saved.Metadata!.Should().ContainKey("branch");
    }

    // ----------------------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------------------

    private async Task<Guid> PostAndExpectAcceptedAsync(string url, string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync(url, content);

        response.StatusCode.Should().Be(
            HttpStatusCode.Accepted,
            $"frozen JSON sent to {url} should be accepted by the server.\nIf this fails, the wire format has been broken — bump the endpoint version, do NOT change the fixture.");

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        doc.RootElement.TryGetProperty("reportId", out var reportIdProp).Should().BeTrue();
        reportIdProp.TryGetGuid(out var reportId).Should().BeTrue();
        reportId.Should().NotBe(Guid.Empty);

        return reportId;
    }
}
