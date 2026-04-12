using System.Text.Json;
using Domovoy.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Domovoy.Tests;

/// <summary>
/// Utility for generating JSON fixtures for IntakeContractTests.
///
/// To capture a fresh JSON payload for a new frozen fixture:
///   dotnet test --filter ContractCaptureUtility --logger "console;verbosity=detailed"
///
/// Then copy the printed JSON into IntakeContractTests.FrozenFixtures as a new
/// raw string literal constant, marked with the FROZEN CONTRACT warning block.
///
/// This class deliberately does NOT use the Database collection or any fixtures —
/// it only serializes typed objects to JSON, so it has no DB or container dependencies.
/// </summary>
public class ContractCaptureUtility
{
    private readonly ITestOutputHelper _output;

    public ContractCaptureUtility(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void PrintSerializedJsonForFixtures()
    {
        // Match the JSON serializer settings that ASP.NET Core uses by default
        // (camelCase property naming, no string enum converter), which is also
        // what HttpClient.PostAsJsonAsync produces. This means the output here
        // matches exactly what DomovoyClient sends over the wire.
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var eventReq = TestDataBuilder.CreateEventReport();
        var errorReq = TestDataBuilder.CreateErrorReport();

        _output.WriteLine("=== EVENT (from TestDataBuilder.CreateEventReport) ===");
        _output.WriteLine(JsonSerializer.Serialize(eventReq, options));
        _output.WriteLine("");
        _output.WriteLine("=== ERROR (from TestDataBuilder.CreateErrorReport) ===");
        _output.WriteLine(JsonSerializer.Serialize(errorReq, options));
    }
}
