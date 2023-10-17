using AISmarteasy.Core.Connecting.MicrosoftGraph;

namespace AISmarteasy.Core.Connecting;

public interface ICalendarConnector
{
    Task<CalendarEvent> AddEventAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default);

    Task<IEnumerable<CalendarEvent>> GetEventsAsync(int? top, int? skip, string? @select, CancellationToken cancellationToken = default);
}
