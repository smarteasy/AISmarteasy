using AISmarteasy.Core.Connector.MicrosoftGraph;

namespace AISmarteasy.Core.Connector;

public interface ICalendarConnector
{
    Task<CalendarEvent> AddEventAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default);

    Task<IEnumerable<CalendarEvent>> GetEventsAsync(int? top, int? skip, string? @select, CancellationToken cancellationToken = default);
}
