using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Domovoy.Client;
using Xunit;

namespace Domovoy.Tests;

/// <summary>
/// Unit tests for how SendAttachmentAsync describes a failed upload. No server or database: the HttpClient runs on a stub handler.
/// </summary>
public class AttachmentUploadResultTests
{
    private class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            _respond = respond;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_respond(request));
        }
    }

    private static DomovoyClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        return new DomovoyClient("http://domovoy.invalid", new HttpClient(new StubHandler(respond)));
    }

    private static MemoryStream Payload()
    {
        return new MemoryStream(Encoding.UTF8.GetBytes("replay data"));
    }

    [Fact]
    public async Task SendAttachmentAsync_ProxyRejects_ReportsStatusBodyAndServer()
    {
        using var client = CreateClient(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.RequestEntityTooLarge)
            {
                Content = new StringContent("<html><title>413 Request Entity Too Large</title></html>")
            };
            response.Headers.Server.ParseAdd("nginx/1.29.0");
            return response;
        });

        var result = await client.SendAttachmentAsync(Guid.CreateVersion7(), Payload(), "replay.zst");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(413);
        result.Server.Should().Be("nginx/1.29.0");
        result.Error.Should().Contain("413 Request Entity Too Large");
    }

    [Fact]
    public async Task SendAttachmentAsync_ConnectionReset_ReportsInnerException()
    {
        using var client = CreateClient(_ => throw new HttpRequestException("Error while copying content to a stream.", new IOException("Connection reset by peer")));

        var result = await client.SendAttachmentAsync(Guid.CreateVersion7(), Payload(), "replay.zst");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().BeNull();
        result.Error.Should().Contain("Error while copying content to a stream.");
        result.Error.Should().Contain("Connection reset by peer");
    }

    [Fact]
    public async Task SendAttachmentAsync_LongErrorBody_Truncated()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(new string('x', 100_000))
        });

        var result = await client.SendAttachmentAsync(Guid.CreateVersion7(), Payload(), "replay.zst");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(500);
        result.Error!.Length.Should().BeLessThan(2000);
    }

    [Fact]
    public async Task SendAttachmentAsync_Accepted_Succeeds()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.Accepted));

        var result = await client.SendAttachmentAsync(Guid.CreateVersion7(), Payload(), "replay.zst");

        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(202);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task SendAttachmentAsync_NoServerHeader_ServerNull()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("missing") });

        var result = await client.SendAttachmentAsync(Guid.CreateVersion7(), Payload(), "replay.zst");

        result.StatusCode.Should().Be(404);
        result.Server.Should().BeNull();
    }

    private class BrokenContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext? context)
        {
            throw new IOException("Connection reset by peer");
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    // A proxy refusing a body commonly sends its status line and headers and then resets the connection, so the status must survive a failed read of the body.
    [Fact]
    public async Task SendAttachmentAsync_BodyReadFails_KeepsStatusAndServer()
    {
        using var client = CreateClient(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.RequestEntityTooLarge) { Content = new BrokenContent() };
            response.Headers.Server.ParseAdd("nginx/1.29.0");
            return response;
        });

        var result = await client.SendAttachmentAsync(Guid.CreateVersion7(), Payload(), "replay.zst");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(413);
        result.Server.Should().Be("nginx/1.29.0");
        result.Error.Should().Contain("Connection reset by peer");
    }
}
