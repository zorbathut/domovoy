using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUlid;
using Domovoy.Client;
using Domovoy.Shared.DTOs;
using Domovoy.Shared.Models;
using Domovoy.Tests.Infrastructure;
using Xunit;
using Environment = Domovoy.Shared.Models.Environment;

namespace Domovoy.Tests;

[Collection("Database")]
public class AttachmentTests : IClassFixture<IntakeApiFactory>, IAsyncLifetime
{
    private readonly IntakeApiFactory _factory;
    private readonly DatabaseFixture _dbFixture;
    private readonly HttpClient _client;
    private readonly string _serverUrl;

    public AttachmentTests(IntakeApiFactory factory, DatabaseFixture dbFixture)
    {
        _factory = factory;
        _dbFixture = dbFixture;
        _client = factory.CreateClient();
        _serverUrl = _client.BaseAddress!.ToString();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _dbFixture.ClearReportsAsync();
    }

    private async Task<Guid> CreateErrorReportAsync()
    {
        var errorReport = TestDataBuilder.CreateErrorReport();
        var response = await _client.PostAsJsonAsync("/api/v1/reports/error", errorReport);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("reportId").GetGuid();
    }

    [Fact]
    public async Task UploadAttachment_ToExistingReport_ReturnsAccepted()
    {
        // Arrange
        var reportId = await CreateErrorReportAsync();
        var fileContent = Encoding.UTF8.GetBytes("test file content");

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(fileContent), "file", "test.txt");

        // Act
        var response = await _client.PostAsync($"/api/v1/reports/{reportId}/attachments", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task UploadAttachment_ToNonexistentReport_ReturnsNotFound()
    {
        // Arrange
        var fakeReportId = Ulid.NewUlid().ToGuid();
        var fileContent = Encoding.UTF8.GetBytes("test file content");

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(fileContent), "file", "test.txt");

        // Act
        var response = await _client.PostAsync($"/api/v1/reports/{fakeReportId}/attachments", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UploadAttachment_WithNoFile_ReturnsBadRequest()
    {
        // Arrange
        var reportId = await CreateErrorReportAsync();

        using var content = new MultipartFormDataContent();
        // Don't add any file

        // Act
        var response = await _client.PostAsync($"/api/v1/reports/{reportId}/attachments", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadAttachment_PersistsMetadataToDatabase()
    {
        // Arrange
        var reportId = await CreateErrorReportAsync();
        var fileContent = Encoding.UTF8.GetBytes("hello world attachment");

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(fileContent), "file", "screenshot.png");

        // Act
        var response = await _client.PostAsync($"/api/v1/reports/{reportId}/attachments", content);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Assert
        await using var dbContext = _dbFixture.CreateDbContext();
        var attachment = await dbContext.Attachments
            .Where(a => a.ReportId == reportId)
            .FirstOrDefaultAsync();

        attachment.Should().NotBeNull();
        attachment!.Filename.Should().Be("screenshot.png");
        attachment.SizeBytes.Should().Be(fileContent.Length);
        attachment.ReportId.Should().Be(reportId);
        attachment.StorageKey.Should().Contain(reportId.ToString());
    }

    [Fact]
    public async Task UploadAttachment_ReturnsAttachmentInfo()
    {
        // Arrange
        var reportId = await CreateErrorReportAsync();
        var fileContent = Encoding.UTF8.GetBytes("test data for info");

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(fileContent), "file", "crash.dmp");

        // Act
        var response = await _client.PostAsync($"/api/v1/reports/{reportId}/attachments", content);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var info = await response.Content.ReadFromJsonAsync<AttachmentInfo>();

        // Assert
        info.Should().NotBeNull();
        info!.Id.Should().NotBe(Guid.Empty);
        info.ReportId.Should().Be(reportId);
        info.Filename.Should().Be("crash.dmp");
        info.SizeBytes.Should().Be(fileContent.Length);
    }

    [Fact]
    public async Task ClientSendAttachmentAsync_EndToEnd()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        using var client = new DomovoyClient(_serverUrl, httpClient);

        var standard = new StandardPayload
        {
            GameVersion = "1.0.0",
            Platform = "Windows",
            Environment = Environment.Dev,
            UserId = Ulid.NewUlid().ToGuid(),
            ComputerId = Ulid.NewUlid().ToGuid(),
            GameId = Ulid.NewUlid().ToGuid(),
            GameSequenceIds = [Ulid.NewUlid().ToGuid()],
            ProcessId = Ulid.NewUlid().ToGuid()
        };

        var errorData = new ErrorPayload
        {
            Severity = Severity.Fatal,
            Message = "Crash with attachment",
            StackTrace = "at Test() in Test.cs:line 1",
            Log = "Test log"
        };

        // Act - Send error, get reportId, then upload attachment
        var reportId = await client.SendErrorAsync(standard, errorData);
        reportId.Should().NotBeNull();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("crash dump data"));
        var success = await client.SendAttachmentAsync(reportId!.Value, stream, "crash.dmp", "application/octet-stream");

        // Assert
        success.Should().BeTrue();

        await using var dbContext = _dbFixture.CreateDbContext();
        var attachment = await dbContext.Attachments
            .Where(a => a.ReportId == reportId.Value)
            .FirstOrDefaultAsync();

        attachment.Should().NotBeNull();
        attachment!.Filename.Should().Be("crash.dmp");
    }
}
