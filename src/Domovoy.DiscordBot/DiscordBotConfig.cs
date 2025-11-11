namespace Domovoy.DiscordBot;

/// <summary>
/// Configuration for the Discord bot, loaded from config/discord-bot.json
/// </summary>
public class DiscordBotConfig
{
    public Guid SubscriberId { get; set; }
    public string BotToken { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
}
