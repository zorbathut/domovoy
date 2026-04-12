using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Domovoy.NotificationClient;
using Domovoy.Shared.DTOs;
using Domovoy.Shared.Models;

namespace Domovoy.DiscordBot;

/// <summary>
/// Background service that polls Domovoy notifications and sends error reports to Discord.
/// </summary>
public class DiscordNotificationService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DiscordNotificationService> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private DiscordSocketClient? _discordClient;
    private DomovoyNotificationClient? _domovoyClient;
    private DiscordBotConfig? _config;
    private ulong _channelId;
    private bool _isReady;

    public DiscordNotificationService(
        IConfiguration configuration,
        ILogger<DiscordNotificationService> logger,
        IHostApplicationLifetime lifetime)
    {
        _configuration = configuration;
        _logger = logger;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Try to load config file
            if (!TryLoadConfig())
            {
                _logger.LogInformation("Discord bot not configured. Exiting gracefully.");
                _lifetime.StopApplication();
                return;
            }

            await InitializeDiscordAsync(stoppingToken);
            await InitializeDomovoyClientAsync(stoppingToken);
            await ProcessNotificationsAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Discord notification service was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error in Discord notification service");
            throw;
        }
    }

    private bool TryLoadConfig()
    {
        var configPath = _configuration["ConfigPath"] ?? "config/discord-bot.json";

        if (!File.Exists(configPath))
        {
            _logger.LogWarning(
                "Discord bot config file not found at {ConfigPath}. " +
                "Copy config/discord-bot.json.example to config/discord-bot.json and configure it.",
                configPath);
            return false;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            _config = JsonSerializer.Deserialize<DiscordBotConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (_config == null)
            {
                _logger.LogError("Failed to deserialize Discord bot config from {ConfigPath}", configPath);
                return false;
            }

            // Validate config
            if (_config.SubscriberId == Guid.Empty)
            {
                _logger.LogError("Invalid subscriberId in config file. Generate a new GUID.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_config.BotToken) || _config.BotToken == "YOUR_DISCORD_BOT_TOKEN_HERE")
            {
                _logger.LogError("Invalid botToken in config file. Get your bot token from Discord Developer Portal.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_config.ChannelId) || _config.ChannelId == "YOUR_DISCORD_CHANNEL_ID_HERE")
            {
                _logger.LogError("Invalid channelId in config file. Set the Discord channel ID where errors will be posted.");
                return false;
            }

            if (!ulong.TryParse(_config.ChannelId, out _channelId))
            {
                _logger.LogError("Invalid channelId format in config file. Must be a valid Discord channel ID (numeric).");
                return false;
            }

            _logger.LogInformation("Successfully loaded Discord bot configuration from {ConfigPath}", configPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Discord bot config from {ConfigPath}", configPath);
            return false;
        }
    }

    private async Task InitializeDiscordAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Initializing Discord client...");

        _discordClient = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Info,
            GatewayIntents = GatewayIntents.Guilds
        });

        _discordClient.Log += LogDiscordMessage;
        _discordClient.Ready += () =>
        {
            _isReady = true;
            _logger.LogInformation("Discord client ready");
            return Task.CompletedTask;
        };

        await _discordClient.LoginAsync(TokenType.Bot, _config!.BotToken);
        await _discordClient.StartAsync();

        // Wait for Discord to be ready
        var timeout = TimeSpan.FromSeconds(30);
        var elapsed = TimeSpan.Zero;
        while (!_isReady && elapsed < timeout && !stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(100, stoppingToken);
            elapsed += TimeSpan.FromMilliseconds(100);
        }

        if (!_isReady)
        {
            throw new TimeoutException("Discord client failed to become ready within 30 seconds");
        }

        _logger.LogInformation("Discord client initialized successfully");
    }

    private async Task InitializeDomovoyClientAsync(CancellationToken stoppingToken)
    {
        var webApiUrl = _configuration["Domovoy:WebApiUrl"] ?? "http://localhost:1975";
        var subscriberName = _configuration["Domovoy:SubscriberName"] ?? "Discord Bot";

        _logger.LogInformation(
            "Initializing Domovoy notification client with subscriber ID {SubscriberId}...",
            _config!.SubscriberId);

        _domovoyClient = new DomovoyNotificationClient(webApiUrl);

        // Retry with exponential backoff if the API is not ready
        const int maxRetries = 10;
        var retryDelay = TimeSpan.FromSeconds(5);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            stoppingToken.ThrowIfCancellationRequested();

            try
            {
                // Try to load existing subscriber, or register if not found
                var loaded = await _domovoyClient.LoadSubscriberAsync(_config.SubscriberId);
                if (loaded)
                {
                    _logger.LogInformation("Loaded existing Domovoy subscriber: {Name}", subscriberName);
                    return;
                }

                _logger.LogInformation("Subscriber not found, registering new subscriber...");
                await _domovoyClient.RegisterAsync(subscriberName, heartbeatTimeoutMinutes: 15);
                _logger.LogInformation("Registered new Domovoy subscriber with ID {SubscriberId}", _domovoyClient.SubscriberId);

                if (_domovoyClient.SubscriberId != _config.SubscriberId)
                {
                    _logger.LogWarning(
                        "Registered subscriber ID {ActualId} doesn't match config {ConfigId}. " +
                        "Update config/discord-bot.json with subscriberId: \"{ActualId}\"",
                        _domovoyClient.SubscriberId,
                        _config.SubscriberId,
                        _domovoyClient.SubscriberId);
                }

                return; // Success
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to connect to Domovoy API (attempt {Attempt}/{MaxRetries}). Retrying in {Delay}s...",
                    attempt,
                    maxRetries,
                    retryDelay.TotalSeconds);

                // Report the first failure to Discord so operators know there's an issue
                if (attempt == 1)
                {
                    await ReportApiConnectionErrorAsync(ex, webApiUrl);
                }

                await Task.Delay(retryDelay, stoppingToken);

                // Exponential backoff with max of 60 seconds
                retryDelay = TimeSpan.FromSeconds(Math.Min(retryDelay.TotalSeconds * 2, 60));
            }
        }

        // If we get here, all retries failed - let the last exception propagate
        throw new InvalidOperationException(
            $"Failed to initialize Domovoy client after {maxRetries} attempts. " +
            "Check that the Web API is running and accessible.");
    }

    private async Task ProcessNotificationsAsync(CancellationToken stoppingToken)
    {
        var heartbeatIntervalMinutes = _configuration.GetValue("NotificationProcessor:HeartbeatIntervalMinutes", 5);
        var pollIntervalSeconds = _configuration.GetValue("NotificationProcessor:PollIntervalSeconds", 1);
        var batchSize = _configuration.GetValue("NotificationProcessor:BatchSize", 100);

        _logger.LogInformation(
            "Starting notification processor (heartbeat: {HeartbeatMin}m, poll: {PollSec}s, batch: {BatchSize})",
            heartbeatIntervalMinutes,
            pollIntervalSeconds,
            batchSize);

        var processor = new NotificationProcessor(
            _domovoyClient!,
            heartbeatInterval: TimeSpan.FromMinutes(heartbeatIntervalMinutes),
            pollInterval: TimeSpan.FromSeconds(pollIntervalSeconds),
            batchSize: batchSize);

        await processor.StartAsync(ProcessNotificationAsync, stoppingToken);
    }

    private const string ReportEventCategory = "Report";

    private async Task<bool> ProcessNotificationAsync(NotificationResponse notification)
    {
        try
        {
            var report = notification.Report;

            if (report.Severity != null)
            {
                if (!ShouldNotifyError(notification)) return true;

                _logger.LogInformation(
                    "Processing error notification {NotificationId}: {Severity} - {Message}",
                    notification.Id, report.Severity, report.Message);

                await SendErrorEmbedAsync(notification);
                return true;
            }

            if (string.Equals(report.Category, ReportEventCategory, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Processing report-event notification {NotificationId}: {Name}",
                    notification.Id, report.Name);

                await SendEventEmbedAsync(notification);
                return true;
            }

            _logger.LogDebug(
                "Skipping notification {NotificationId} (type={ReportType}, category={Category})",
                notification.Id, report.ReportType, report.Category);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process notification {NotificationId}", notification.Id);
            return false; // Don't ACK on failure, will retry
        }
    }

    private bool ShouldNotifyError(NotificationResponse notification)
    {
        var report = notification.Report;

        if (report.Severity < Severity.Warning)
        {
            _logger.LogDebug(
                "Skipping error notification {NotificationId} with severity {Severity}",
                notification.Id, report.Severity);
            return false;
        }

        if (report.Environment != "Release")
        {
            _logger.LogDebug(
                "Skipping error notification {NotificationId} from non-Release environment {Environment}",
                notification.Id, report.Environment);
            return false;
        }

        return true;
    }

    private async Task SendErrorEmbedAsync(NotificationResponse notification)
    {
        var report = notification.Report;

        var color = report.Severity switch
        {
            Severity.Fatal => Color.DarkRed,
            Severity.Error => Color.Red,
            Severity.Warning => Color.Orange,
            _ => Color.LightGrey
        };

        var stackTrace = Truncate(report.StackTrace ?? "", 1000);

        var embed = new EmbedBuilder()
            .WithTitle($"{report.Severity} Reported")
            .WithDescription(report.Message)
            .WithColor(color)
            .WithCurrentTimestamp()
            .AddField("Platform", report.Platform, inline: true)
            .AddField("Version", report.Version, inline: true)
            .AddField("Environment", report.Environment, inline: true)
            .AddField("Report Time", report.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC"), inline: true)
            .AddField("Stack Trace", $"```\n{stackTrace}\n```", inline: false);

        AddDetailUrl(embed, "crashes", report.Id);

        if (!string.IsNullOrEmpty(report.Log))
        {
            embed.AddField("Log", $"```\n{Truncate(report.Log, 500)}\n```", inline: false);
        }

        embed.WithFooter($"Report ID: {report.Id} | Notification ID: {notification.Id}");

        await SendAsync(embed);

        _logger.LogInformation(
            "Sent error notification to Discord channel {ChannelId}: {Severity} - {Message}",
            _channelId, report.Severity, report.Message);
    }

    private async Task SendEventEmbedAsync(NotificationResponse notification)
    {
        var report = notification.Report;

        var embed = new EmbedBuilder()
            .WithTitle($"Report: {report.Name}")
            .WithColor(Color.Blue)
            .WithCurrentTimestamp()
            .AddField("Platform", report.Platform, inline: true)
            .AddField("Version", report.Version, inline: true)
            .AddField("Environment", report.Environment, inline: true)
            .AddField("Report Time", report.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC"), inline: true);

        if (report.Data is { Count: > 0 })
        {
            var dataLines = string.Join("\n",
                report.Data.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}: {kvp.Value}"));
            embed.AddField("Data", $"```\n{Truncate(dataLines, 1000)}\n```", inline: false);
        }

        AddDetailUrl(embed, "events", report.Id);

        embed.WithFooter($"Report ID: {report.Id} | Notification ID: {notification.Id}");

        await SendAsync(embed);

        _logger.LogInformation(
            "Sent report-event notification to Discord channel {ChannelId}: {Name}",
            _channelId, report.Name);
    }

    private void AddDetailUrl(EmbedBuilder embed, string segment, Guid reportId)
    {
        if (!string.IsNullOrWhiteSpace(_config!.WebUiUrl))
        {
            embed.WithUrl($"{_config.WebUiUrl.TrimEnd('/')}/{segment}/{reportId}");
        }
    }

    private async Task SendAsync(EmbedBuilder embed)
    {
        var channel = await _discordClient!.GetChannelAsync(_channelId) as IMessageChannel
            ?? throw new InvalidOperationException($"Cannot find Discord channel with ID {_channelId}");

        string? messageContent = null;
        if (!string.IsNullOrWhiteSpace(_config!.MentionRoleId))
        {
            messageContent = $"<@&{_config.MentionRoleId}>";
        }

        await channel.SendMessageAsync(text: messageContent, embed: embed.Build());
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";

    private async Task ReportApiConnectionErrorAsync(HttpRequestException ex, string webApiUrl)
    {
        try
        {
            var channel = await _discordClient!.GetChannelAsync(_channelId) as IMessageChannel;
            if (channel == null)
            {
                _logger.LogWarning("Cannot report API error to Discord: channel {ChannelId} not found", _channelId);
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("Domovoy API Connection Error")
                .WithDescription($"Failed to connect to the Domovoy API. The bot will retry automatically.")
                .WithColor(Color.Orange)
                .WithCurrentTimestamp()
                .AddField("API URL", webApiUrl, inline: true)
                .AddField("Error", ex.Message, inline: false)
                .WithFooter("The bot will continue retrying with exponential backoff");

            await channel.SendMessageAsync(embed: embed.Build());

            _logger.LogInformation("Reported API connection error to Discord channel {ChannelId}", _channelId);
        }
        catch (Exception reportEx)
        {
            // Don't let Discord reporting failure break the retry loop
            _logger.LogWarning(reportEx, "Failed to report API connection error to Discord");
        }
    }

    private Task LogDiscordMessage(LogMessage message)
    {
        var logLevel = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel, message.Exception, "[Discord] {Message}", message.Message);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Discord notification service...");

        if (_discordClient != null)
        {
            await _discordClient.StopAsync();
            await _discordClient.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}
