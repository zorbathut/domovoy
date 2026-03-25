using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace Domovoy.Tests.Infrastructure;

/// <summary>
/// Manages a PostgreSQL Docker container for integration tests.
/// Adapted from Ilmarinen's TestPostgresContainer.
/// </summary>
public class TestPostgresContainer : IAsyncDisposable
{
    private const string PostgresImage = "postgres:16-alpine";
    private const string PidLabel = "domovoy.test.pid";

    private readonly DockerClient _client;
    private string? _containerId;

    public string Database { get; }
    public string Username { get; }
    public string Password { get; }
    public int Port { get; private set; }

    public string ConnectionString =>
        $"Host=localhost;Port={Port};Database={Database};Username={Username};Password={Password}";

    public string AdminConnectionString =>
        $"Host=localhost;Port={Port};Database=postgres;Username={Username};Password={Password}";

    public TestPostgresContainer(
        string database = "domovoy_test",
        string username = "test",
        string password = "test")
    {
        Database = database;
        Username = username;
        Password = password;
        _client = CreateDockerClient();
    }

    public async Task StartAsync()
    {
        await PullImageIfNeededAsync();

        var pid = Environment.ProcessId;

        var response = await _client.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = PostgresImage,
            Labels = new Dictionary<string, string>
            {
                [PidLabel] = pid.ToString()
            },
            Env =
            [
                $"POSTGRES_DB={Database}",
                $"POSTGRES_USER={Username}",
                $"POSTGRES_PASSWORD={Password}",
                "POSTGRES_INITDB_ARGS=--nosync",
                "PGDATA=/dev/shm/pgdata"
            ],
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    ["5432/tcp"] = [new() { HostPort = "" }]
                },
                AutoRemove = true,
                Tmpfs = new Dictionary<string, string>
                {
                    ["/dev/shm"] = "rw,nosuid,nodev,size=256m"
                }
            }
        });

        _containerId = response.ID;
        await _client.Containers.StartContainerAsync(_containerId, new ContainerStartParameters());

        var inspect = await _client.Containers.InspectContainerAsync(_containerId);
        var hostPort = inspect.NetworkSettings.Ports["5432/tcp"].First().HostPort;
        Port = int.Parse(hostPort);

        await WaitForReadyAsync();
    }

    private async Task WaitForReadyAsync()
    {
        var warnAfter = TimeSpan.FromSeconds(30);
        var timeout = TimeSpan.FromMinutes(10);
        var warned = false;
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            try
            {
                using var tcp = new TcpClient();
                await tcp.ConnectAsync("localhost", Port);

                await using var conn = new Npgsql.NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();
                return;
            }
            catch
            {
                if (!warned && DateTime.UtcNow - start > warnAfter)
                {
                    warned = true;
                    Console.WriteLine(
                        $"WARNING: PostgreSQL container on port {Port} not ready after {warnAfter.TotalSeconds}s, still waiting...");
                }
                await Task.Delay(100);
            }
        }

        throw new TimeoutException($"PostgreSQL container did not become ready within {timeout}");
    }

    private async Task PullImageIfNeededAsync()
    {
        try
        {
            await _client.Images.InspectImageAsync(PostgresImage);
        }
        catch (DockerImageNotFoundException)
        {
            await _client.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = PostgresImage },
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

    internal static DockerClient CreateDockerClient()
    {
        var dockerHost = Environment.GetEnvironmentVariable("DOCKER_HOST");

        if (!string.IsNullOrEmpty(dockerHost))
            return new DockerClientConfiguration(new Uri(dockerHost)).CreateClient();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();

        return new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
    }

    /// <summary>
    /// Cleans up stale containers from crashed test processes and spawns a watchdog
    /// for the current process. Call once before starting containers.
    /// </summary>
    internal static async Task CleanupStaleContainersAsync()
    {
        using var client = CreateDockerClient();

        var containers = await client.Containers.ListContainersAsync(new ContainersListParameters
        {
            All = true,
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                ["label"] = new Dictionary<string, bool> { [PidLabel] = true }
            }
        });

        foreach (var container in containers)
        {
            if (!container.Labels.TryGetValue(PidLabel, out var pidStr)
                || !int.TryParse(pidStr, out var pid))
                continue;

            try
            {
                Process.GetProcessById(pid);
                continue; // Still running
            }
            catch (ArgumentException)
            {
                // Process is gone — container is stale
            }

            try
            {
                await client.Containers.StopContainerAsync(container.ID,
                    new ContainerStopParameters { WaitBeforeKillSeconds = 1 });
            }
            catch
            {
                // Best effort
            }
        }
    }

    /// <summary>
    /// Spawns a detached watchdog that kills all domovoy test containers if the test process dies.
    /// </summary>
    internal static void SpawnWatchdog()
    {
        var pid = Environment.ProcessId;

        Process.Start(new ProcessStartInfo
        {
            FileName = "setsid",
            Arguments = $"-f /bin/sh -c \"while kill -0 {pid} 2>/dev/null; do sleep 1; done; docker rm -f $(docker ps -aq --filter label={PidLabel}={pid}) 2>/dev/null\" </dev/null >/dev/null 2>&1",
            UseShellExecute = false,
            CreateNoWindow = true
        });
    }
}
