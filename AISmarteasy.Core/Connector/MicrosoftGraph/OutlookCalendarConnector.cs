using Microsoft.Graph;

namespace AISmarteasy.Core.Connector.MicrosoftGraph;

public class OutlookCalendarConnector : ICalendarConnector
{
    private readonly GraphServiceClient _graphServiceClient;

    public OutlookCalendarConnector(GraphServiceClient graphServiceClient)
    {
        _graphServiceClient = graphServiceClient;
    }

    public async Task<CalendarEvent> AddEventAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default)
    {
        Event resultEvent = await _graphServiceClient.Me.Events.Request()
            .AddAsync(calendarEvent.ToGraphEvent(), cancellationToken).ConfigureAwait(false);
        return resultEvent.ToCalendarEvent();
    }

    public async Task<IEnumerable<CalendarEvent>> GetEventsAsync(
        int? top, int? skip, string? select, CancellationToken cancellationToken = default)
    {
        ICalendarEventsCollectionRequest query = _graphServiceClient.Me.Calendar.Events.Request();

        if (top.HasValue)
        {
            query.Top(top.Value);
        }

        if (skip.HasValue)
        {
            query.Skip(skip.Value);
        }

        if (!string.IsNullOrEmpty(select))
        {
            query.Select(select);
        }

        ICalendarEventsCollectionPage result = await query.GetAsync(cancellationToken).ConfigureAwait(false);

        IEnumerable<CalendarEvent> events = result.Select(e => e.ToCalendarEvent());

        return events;
    }
}
