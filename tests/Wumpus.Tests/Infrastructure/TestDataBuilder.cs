using Wumpus.Shared.DTOs;
using Wumpus.Shared.Models;

namespace Wumpus.Tests.Infrastructure;

/// <summary>
/// Builder class for creating test crash report data with sensible defaults.
/// </summary>
public class TestDataBuilder
{
    /// <summary>
    /// Creates a default crash report request with typical values.
    /// </summary>
    public static SubmitCrashReportRequest CreateCrashReport(
        string? gameVersion = null,
        string? platform = null,
        string? exceptionType = null,
        string? exceptionMessage = null,
        string? stackTrace = null,
        Dictionary<string, string>? systemInfo = null,
        Dictionary<string, string>? userContext = null)
    {
        return new SubmitCrashReportRequest
        {
            Core = new CrashReportCore
            {
                GameVersion = gameVersion ?? "1.0.0",
                Platform = platform ?? "Windows",
                ExceptionType = exceptionType ?? "System.NullReferenceException",
                ExceptionMessage = exceptionMessage ?? "Object reference not set to an instance of an object.",
                StackTrace = stackTrace ?? CreateDefaultStackTrace()
            },
            SystemInfo = systemInfo ?? CreateDefaultSystemInfo(),
            UserContext = userContext ?? CreateDefaultUserContext()
        };
    }

    /// <summary>
    /// Creates a crash report with a specific stack trace for testing deduplication.
    /// </summary>
    public static SubmitCrashReportRequest CreateCrashReportWithStackTrace(string stackTrace)
    {
        return CreateCrashReport(stackTrace: stackTrace);
    }

    /// <summary>
    /// Creates a crash report that will have a different hash (different stack trace).
    /// </summary>
    public static SubmitCrashReportRequest CreateDifferentCrashReport()
    {
        return CreateCrashReport(
            exceptionType: "System.ArgumentException",
            exceptionMessage: "Value cannot be null. (Parameter 'value')",
            stackTrace: @"   at Game.Utils.Validator.CheckNotNull(String value) in C:\Game\Utils\Validator.cs:line 10
   at Game.Systems.InputHandler.ProcessInput(String input) in C:\Game\Systems\InputHandler.cs:line 25
   at Game.Core.GameLoop.Update() in C:\Game\Core\GameLoop.cs:line 50
   at Game.Program.Main() in C:\Game\Program.cs:line 15");
    }

    /// <summary>
    /// Creates a crash report with invalid data (empty required fields).
    /// </summary>
    public static SubmitCrashReportRequest CreateInvalidCrashReport()
    {
        return new SubmitCrashReportRequest
        {
            Core = new CrashReportCore
            {
                GameVersion = "",
                Platform = "",
                ExceptionType = "",
                ExceptionMessage = "",
                StackTrace = ""
            }
        };
    }

    private static string CreateDefaultStackTrace()
    {
        return @"   at Game.Player.PlayerController.Move(Vector3 direction) in C:\Game\Player\PlayerController.cs:line 42
   at Game.Player.PlayerInputHandler.HandleInput() in C:\Game\Player\PlayerInputHandler.cs:line 78
   at Game.Core.GameLoop.Update() in C:\Game\Core\GameLoop.cs:line 120
   at Game.Core.GameLoop.Run() in C:\Game\Core\GameLoop.cs:line 55
   at Game.Program.Main(String[] args) in C:\Game\Program.cs:line 18
   at System.AppDomain.ExecuteAssembly(String assemblyFile)
   at Microsoft.VisualStudio.HostingProcess.HostProc.RunUsersAssembly()";
    }

    private static Dictionary<string, string> CreateDefaultSystemInfo()
    {
        return new Dictionary<string, string>
        {
            { "OS", "Windows 10 Pro 64-bit" },
            { "CPU", "Intel Core i7-9700K @ 3.60GHz" },
            { "RAM", "16 GB" },
            { "GPU", "NVIDIA GeForce RTX 2070" },
            { "Resolution", "1920x1080" }
        };
    }

    private static Dictionary<string, string> CreateDefaultUserContext()
    {
        return new Dictionary<string, string>
        {
            { "UserId", "user_12345" },
            { "SessionId", "session_67890" },
            { "Level", "Level_3" },
            { "PlayTime", "145.5" }
        };
    }
}
