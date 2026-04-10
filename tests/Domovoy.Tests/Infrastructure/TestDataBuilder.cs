using System;
using Domovoy.Shared.DTOs;
using Domovoy.Shared.Models;

namespace Domovoy.Tests.Infrastructure;

/// <summary>
/// Builder class for creating test report data with sensible defaults.
/// </summary>
public class TestDataBuilder
{
    /// <summary>
    /// Creates a default event report request.
    /// </summary>
    public static SubmitEventRequest CreateEventReport(
        string? version = null,
        string? platform = null,
        string? eventName = null,
        string? category = null,
        Domovoy.Shared.Models.Environment? environment = null,
        Guid? userId = null,
        Guid? computerId = null,
        Guid? campaignId = null)
    {
        return new SubmitEventRequest
        {
            Version = version ?? "1.0.0",
            Platform = platform ?? "Windows",
            Environment = environment ?? Domovoy.Shared.Models.Environment.Dev,
            UserId = userId ?? Guid.CreateVersion7(),
            ComputerId = computerId ?? Guid.CreateVersion7(),
            CampaignId = campaignId ?? Guid.CreateVersion7(),
            CampaignSequenceIds = [Guid.CreateVersion7()],
            ProcessId = Guid.CreateVersion7(),
            Category = category ?? "TestCategory",
            Name = eventName ?? "TestEvent"
        };
    }

    /// <summary>
    /// Creates a default error report request with typical crash values.
    /// </summary>
    public static SubmitErrorRequest CreateErrorReport(
        string? version = null,
        string? platform = null,
        string? message = null,
        string? stackTrace = null,
        string? log = null,
        Severity? severity = null,
        Domovoy.Shared.Models.Environment? environment = null,
        Guid? userId = null,
        Guid? computerId = null,
        Guid? campaignId = null)
    {
        return new SubmitErrorRequest
        {
            Version = version ?? "1.0.0",
            Platform = platform ?? "Windows",
            Environment = environment ?? Domovoy.Shared.Models.Environment.Dev,
            UserId = userId ?? Guid.CreateVersion7(),
            ComputerId = computerId ?? Guid.CreateVersion7(),
            CampaignId = campaignId ?? Guid.CreateVersion7(),
            CampaignSequenceIds = [Guid.CreateVersion7()],
            ProcessId = Guid.CreateVersion7(),
            Severity = severity ?? Severity.Fatal,
            Message = message ?? "Object reference not set to an instance of an object.",
            StackTrace = stackTrace ?? CreateDefaultStackTrace(),
            Log = log ?? CreateDefaultLog()
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
            Version = "",
            Platform = "",
            Environment = Domovoy.Shared.Models.Environment.Dev,
            UserId = Guid.Empty,
            ComputerId = Guid.Empty,
            CampaignId = Guid.Empty,
            CampaignSequenceIds = [],
            ProcessId = Guid.Empty,
            Severity = Severity.Error,
            Message = "",
            StackTrace = "",
            Log = ""
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
