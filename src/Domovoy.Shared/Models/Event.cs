namespace Domovoy.Shared.Models;

public class Event : Report
{
    public Event()
    {
        ReportType = ReportType.Event;
    }

    public required EventPayload Data { get; set; }
}
