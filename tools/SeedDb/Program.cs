using Wumpus.Client;
using Wumpus.Shared.DTOs;
using Wumpus.Shared.Models;

Console.WriteLine("Wumpus Database Seeder");
Console.WriteLine("======================\n");

// Use localhost Intake API by default
var serverUrl = args.Length > 0
    ? args[0]
    : "http://localhost:5001";

Console.WriteLine($"Server: {serverUrl}");
Console.WriteLine("Generating and sending seed data...\n");

var random = new Random();
var now = DateTime.UtcNow;

// Generate data for the past 7 days
var startDate = now.AddDays(-7);

var platforms = new[] { "Windows", "Linux", "macOS", "Android", "iOS" };
var versions = new[] { "1.0.0", "1.1.0", "1.2.0", "2.0.0", "2.1.0" };
var environments = new[] { Wumpus.Shared.Models.Environment.Dev, Wumpus.Shared.Models.Environment.Release };

var eventCategories = new[] { "Gameplay", "UI", "Multiplayer", "Combat", "Progression" };
var eventNames = new[] {
    "LevelCompleted", "PlayerDied", "ItemPurchased", "AchievementUnlocked",
    "ButtonClicked", "MenuOpened", "MatchStarted", "EnemyDefeated", "QuestCompleted"
};

var errorMessages = new[] {
    "Failed to load texture",
    "Network connection timeout",
    "Invalid player state",
    "Null reference encountered",
    "Out of memory",
    "Asset not found",
    "Failed to save progress",
    "Database connection failed"
};

var exceptionTypes = new[] {
    "NullReferenceException",
    "TimeoutException",
    "FileNotFoundException",
    "OutOfMemoryException",
    "InvalidOperationException",
    "ArgumentException"
};

int errorCount = 0;
int eventCount = 0;
int failedCount = 0;

// Create client options
var clientOptions = new WumpusClientOptions
{
    ServerUrl = serverUrl,
    AppVersion = "1.0.0",
    Platform = "Windows",
    Environment = Wumpus.Shared.Models.Environment.Dev,
    UserId = Guid.NewGuid(),
    ComputerId = Guid.NewGuid(),
    GameId = Guid.NewGuid()
};

using var client = new WumpusClient(clientOptions);

Console.WriteLine("Sending 100 events...");

// Generate and send 100 events
for (int i = 0; i < 100; i++)
{
    var platform = platforms[random.Next(platforms.Length)];
    var version = versions[random.Next(versions.Length)];
    var environment = environments[random.Next(environments.Length)];
    var eventName = eventNames[random.Next(eventNames.Length)];
    var category = eventCategories[random.Next(eventCategories.Length)];

    var eventRequest = new SubmitEventRequest
    {
        Standard = new StandardPayload
        {
            GameVersion = version,
            Platform = platform,
            Environment = environment,
            UserId = Guid.NewGuid(),
            ComputerId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            SequenceId = Guid.NewGuid()
        },
        Data = new EventPayload
        {
            Name = eventName,
            Category = category,
            Value = random.Next(1, 1000),
            UserId = $"user_{random.Next(1, 50)}",
            Metadata = new Dictionary<string, object>
            {
                { "duration", random.Next(10, 300) },
                { "score", random.Next(100, 10000) }
            }
        }
    };

    var success = await client.SendEventAsync(eventRequest);
    if (success)
    {
        eventCount++;
        if (eventCount % 20 == 0)
            Console.WriteLine($"  Sent {eventCount} events...");
    }
    else
    {
        failedCount++;
    }
}

Console.WriteLine($"\nSending 50 errors...");

// Generate and send 50 errors
for (int i = 0; i < 50; i++)
{
    var platform = platforms[random.Next(platforms.Length)];
    var version = versions[random.Next(versions.Length)];
    var environment = environments[random.Next(environments.Length)];
    var message = errorMessages[random.Next(errorMessages.Length)];
    var exceptionType = exceptionTypes[random.Next(exceptionTypes.Length)];
    var severity = (Severity)random.Next(1, 4); // Warning, Error, or Fatal

    var stackTrace = $@"   at Game.Core.{exceptionType.Replace("Exception", "")}.Method() in Game.cs:line {random.Next(10, 500)}
   at Game.Systems.Manager.Update() in Manager.cs:line {random.Next(10, 200)}
   at Game.Main.Loop() in Main.cs:line {random.Next(10, 100)}";

    var log = $"{exceptionType}: {message}\n{stackTrace}";

    var errorRequest = new SubmitErrorRequest
    {
        Standard = new StandardPayload
        {
            GameVersion = version,
            Platform = platform,
            Environment = environment,
            UserId = Guid.NewGuid(),
            ComputerId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            SequenceId = Guid.NewGuid()
        },
        Data = new ErrorPayload
        {
            Severity = severity,
            Message = message,
            StackTrace = stackTrace,
            Log = log
        }
    };

    var success = await client.SendErrorAsync(errorRequest);
    if (success)
    {
        errorCount++;
        if (errorCount % 10 == 0)
            Console.WriteLine($"  Sent {errorCount} errors...");
    }
    else
    {
        failedCount++;
    }
}

Console.WriteLine($"\n✓ Successfully seeded database!");
Console.WriteLine($"  - {eventCount} events sent");
Console.WriteLine($"  - {errorCount} errors sent");
if (failedCount > 0)
    Console.WriteLine($"  - {failedCount} failed (check server logs)");
Console.WriteLine($"  - Spread across {platforms.Length} platforms");
Console.WriteLine($"  - {versions.Length} versions");
Console.WriteLine($"  - {environments.Length} environments");
