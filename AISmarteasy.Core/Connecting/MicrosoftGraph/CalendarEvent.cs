namespace AISmarteasy.Core.Connecting.MicrosoftGraph;

public class CalendarEvent
{
    public string? Subject { get; set; }

    public string? Content { get; set; }

    public DateTimeOffset? Start { get; set; }

    public DateTimeOffset? End { get; set; }

    public string? Location { get; set; }

    public IEnumerable<string>? Attendees { get; set; } = Enumerable.Empty<string>();
}
