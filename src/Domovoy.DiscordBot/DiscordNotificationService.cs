using System;
using System.IO;
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
            await InitializeDomovoyClientAsync();
            await ProcessNotificationsAsync(stoppingToken);
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

    private async Task InitializeDomovoyClientAsync()
    {
        var webApiUrl = _configuration["Domovoy:WebApiUrl"] ?? "http://localhost:1975";
        var subscriberName = _configuration["Domovoy:SubscriberName"] ?? "Discord Bot";

        _logger.LogInformation(
            "Initializing Domovoy notification client with subscriber ID {SubscriberId}...",
            _config!.SubscriberId);

        _domovoyClient = new DomovoyNotificationClient(webApiUrl);

        // Try to load existing subscriber, or register if not found
        var loaded = await _domovoyClient.LoadSubscriberAsync(_config.SubscriberId);
        if (loaded)
        {
            _logger.LogInformation("Loaded existing Domovoy subscriber: {Name}", subscriberName);
        }
        else
        {
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
        }
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

    private async Task<bool> ProcessNotificationAsync(NotificationResponse notification)
    {
        try
        {
            // Only process error reports (ignore events)
            if (notification.Report.ErrorData == null)
            {
                _logger.LogDebug("Skipping non-error notification {NotificationId}", notification.Id);
                return true; // ACK it to remove from queue
            }

            var error = notification.Report.ErrorData;

            // Only process warnings and above (ignore Info and Unknown)
            if (error.Severity < Severity.Warning)
            {
                _logger.LogDebug(
                    "Skipping error notification {NotificationId} with severity {Severity}",
                    notification.Id,
                    error.Severity);
                return true; // ACK it to remove from queue
            }

            // Only process errors from Release environment
            if (notification.Report.Standard.Environment != Domovoy.Shared.Models.Environment.Release)
            {
                _logger.LogDebug(
                    "Skipping error notification {NotificationId} from non-Release environment {Environment}",
                    notification.Id,
                    notification.Report.Standard.Environment);
                return true; // ACK it to remove from queue
            }

            _logger.LogInformation(
                "Processing error notification {NotificationId}: {Severity} - {Message}",
                notification.Id,
                error.Severity,
                error.Message);

            await SendToDiscordAsync(notification);

            return true; // ACK on success
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process notification {NotificationId}", notification.Id);
            return false; // Don't ACK on failure, will retry
        }
    }

    private async Task SendToDiscordAsync(NotificationResponse notification)
    {
        var channel = await _discordClient!.GetChannelAsync(_channelId) as IMessageChannel;
        if (channel == null)
        {
            throw new InvalidOperationException($"Cannot find Discord channel with ID {_channelId}");
        }

        var error = notification.Report.ErrorData!;
        var standard = notification.Report.Standard;

        // Determine embed color based on severity
        var color = error.Severity switch
        {
            Severity.Fatal => Color.DarkRed,
            Severity.Error => Color.Red,
            Severity.Warning => Color.Orange,
            _ => Color.LightGrey
        };

        // Truncate stack trace if too long (Discord has 1024 char field limit)
        var stackTrace = error.StackTrace;
        if (stackTrace.Length > 1000)
        {
            stackTrace = stackTrace.Substring(0, 997) + "...";
        }

        var embed = new EmbedBuilder()
            .WithTitle($"{error.Severity} Reported")
            .WithDescription(error.Message)
            .WithColor(color)
            .WithCurrentTimestamp()
            .AddField("Platform", standard.Platform, inline: true)
            .AddField("Game Version", standard.GameVersion, inline: true)
            .AddField("Environment", standard.Environment.ToString(), inline: true)
            .AddField("Report Time", notification.Report.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC"), inline: true)
            .AddField("Stack Trace", $"```\n{stackTrace}\n```", inline: false);

        // Add link to detail page if webUiUrl is configured
        if (!string.IsNullOrWhiteSpace(_config!.WebUiUrl))
        {
            var detailUrl = $"{_config.WebUiUrl.TrimEnd('/')}/crashes/{notification.Report.Id}";
            embed.WithUrl(detailUrl);
        }

        // Add log if present and not too long
        if (!string.IsNullOrEmpty(error.Log))
        {
            var log = error.Log;
            if (log.Length > 500)
            {
                log = log.Substring(0, 497) + "...";
            }
            embed.AddField("Log", $"```\n{log}\n```", inline: false);
        }

        embed.WithFooter($"Report ID: {notification.Report.Id} | Notification ID: {notification.Id}");

        // Build message with optional role mention
        string? messageContent = null;
        if (!string.IsNullOrWhiteSpace(_config!.MentionRoleId))
        {
            messageContent = $"<@&{_config.MentionRoleId}>";
        }

        await channel.SendMessageAsync(text: messageContent, embed: embed.Build());

        _logger.LogInformation(
            "Sent error notification to Discord channel {ChannelId}: {Severity} - {Message}",
            _channelId,
            error.Severity,
            error.Message);
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
