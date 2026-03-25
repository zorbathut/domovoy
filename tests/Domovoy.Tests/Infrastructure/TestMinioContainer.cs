using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace Domovoy.Tests.Infrastructure;

/// <summary>
/// Manages a MinIO Docker container for integration tests.
/// </summary>
public class TestMinioContainer : IAsyncDisposable
{
    private const string MinioImage = "minio/minio:latest";
    private const string PidLabel = "domovoy.test.pid";

    private readonly DockerClient _client;
    private string? _containerId;

    public string AccessKey { get; }
    public string SecretKey { get; }
    public int Port { get; private set; }

    public string Endpoint => $"localhost:{Port}";

    public TestMinioContainer(
        string accessKey = "domovoy",
        string secretKey = "domovoy123")
    {
        AccessKey = accessKey;
        SecretKey = secretKey;
        _client = TestPostgresContainer.CreateDockerClient();
    }

    public async Task StartAsync()
    {
        await PullImageIfNeededAsync();

        var pid = Environment.ProcessId;

        var response = await _client.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = MinioImage,
            Cmd = ["server", "/data"],
            Labels = new Dictionary<string, string>
            {
                [PidLabel] = pid.ToString()
            },
            Env =
            [
                $"MINIO_ROOT_USER={AccessKey}",
                $"MINIO_ROOT_PASSWORD={SecretKey}"
            ],
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    ["9000/tcp"] = [new() { HostPort = "" }]
                },
                AutoRemove = true
            }
        });

        _containerId = response.ID;
        await _client.Containers.StartContainerAsync(_containerId, new ContainerStartParameters());

        var inspect = await _client.Containers.InspectContainerAsync(_containerId);
        var hostPort = inspect.NetworkSettings.Ports["9000/tcp"].First().HostPort;
        Port = int.Parse(hostPort);

        await WaitForReadyAsync();
    }

    private async Task WaitForReadyAsync()
    {
        var warnAfter = TimeSpan.FromSeconds(30);
        var timeout = TimeSpan.FromMinutes(10);
        var warned = false;
        var start = DateTime.UtcNow;

        using var http = new HttpClient();

        while (DateTime.UtcNow - start < timeout)
        {
            try
            {
                var response = await http.GetAsync($"http://localhost:{Port}/minio/health/live");
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch
            {
                if (!warned && DateTime.UtcNow - start > warnAfter)
                {
                    warned = true;
                    Console.WriteLine(
                        $"WARNING: MinIO container on port {Port} not ready after {warnAfter.TotalSeconds}s, still waiting...");
                }
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"MinIO container did not become ready within {timeout}");
    }

    private async Task PullImageIfNeededAsync()
    {
        try
        {
            await _client.Images.InspectImageAsync(MinioImage);
        }
        catch (DockerImageNotFoundException)
        {
            await _client.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = MinioImage },
                null,
                new Progress<JSONMessage>());
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_containerId != null)
        {
            try
            {
                await _client.Containers.StopContainerAsync(_containerId, new ContainerStopParameters());
            }
            catch
            {
                // Container might already be stopped/removed (AutoRemove=true)
            }
        }
        _client.Dispose();
    }
}
