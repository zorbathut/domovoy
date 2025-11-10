using Wumpus.Shared.DTOs;
using Wumpus.Shared.Models;

namespace Wumpus.Tests.Infrastructure;

/// <summary>
/// Builder class for creating test error report data with sensible defaults.
/// </summary>
public class TestDataBuilder
{
    /// <summary>
    /// Creates a default error report request with typical crash values.
    /// </summary>
    public static SubmitErrorRequest CreateErrorReport(
        string? gameVersion = null,
        string? platform = null,
        string? message = null,
        string? stackTrace = null,
        string? log = null,
        Severity? severity = null,
        Guid? userId = null,
        Guid? computerId = null,
        Guid? gameId = null)
    {
        return new SubmitErrorRequest
        {
            Standard = new StandardPayload
            {
                GameVersion = gameVersion ?? "1.0.0",
                Platform = platform ?? "Windows",
                UserId = userId ?? Guid.NewGuid(),
                ComputerId = computerId ?? Guid.NewGuid(),
                GameId = gameId ?? Guid.NewGuid(),
                SequenceId = Guid.NewGuid()
            },
            Data = new ErrorPayload
            {
                Severity = severity ?? Severity.Fatal,
                Message = message ?? "Object reference not set to an instance of an object.",
                StackTrace = stackTrace ?? CreateDefaultStackTrace(),
                Log = log ?? CreateDefaultLog()
            }
        };
    }

    /// <summary>
    /// Creates an error report with a specific stack trace for testing.
    /// </summary>
    public static SubmitErrorRequest CreateErrorReportWithStackTrace(string stackTrace)
    {
        return CreateErrorReport(stackTrace: stackTrace);
    }

    /// <summary>
    /// Creates an error report with a different exception and stack trace.
    /// </summary>
    public static SubmitErrorRequest CreateDifferentErrorReport()
    {
        var stackTrace = @"   at Game.Utils.Validator.CheckNotNull(String value) in C:\Game\Utils\Validator.cs:line 10
   at Game.Systems.InputHandler.ProcessInput(String input) in C:\Game\Systems\InputHandler.cs:line 25
   at Game.Core.GameLoop.Update() in C:\Game\Core\GameLoop.cs:line 50
   at Game.Program.Main() in C:\Game\Program.cs:line 15";

        return CreateErrorReport(
            message: "Value cannot be null. (Parameter 'value')",
            stackTrace: stackTrace,
            log: $"System.ArgumentException: Value cannot be null. (Parameter 'value')\n{stackTrace}");
    }

    /// <summary>
    /// Creates an error report with invalid data (empty required fields).
    /// Note: Severity is always valid since it's an enum.
    /// </summary>
    public static SubmitErrorRequest CreateInvalidErrorReport()
    {
        return new SubmitErrorRequest
        {
            Standard = new StandardPayload
            {
                GameVersion = "",
                Platform = "",
                UserId = Guid.Empty,
                ComputerId = Guid.Empty,
                GameId = Guid.Empty,
                SequenceId = Guid.Empty
            },
            Data = new ErrorPayload
            {
                Severity = Severity.Error,
                Message = "",
                StackTrace = "",
                Log = ""
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

    private static string CreateDefaultLog()
    {
        return $"System.NullReferenceException: Object reference not set to an instance of an object.\n{CreateDefaultStackTrace()}";
    }
}
