namespace Wumpus.Shared.Models;

public class Error : Report
{
    public Error()
    {
        ReportType = ReportType.Error;
    }

    public required ErrorPayload Data { get; set; }
}
