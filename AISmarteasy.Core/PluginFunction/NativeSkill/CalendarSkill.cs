using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using AISmarteasy.Core.Connecting;
using AISmarteasy.Core.Connecting.MicrosoftGraph;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISmarteasy.Core.PluginFunction.NativeSkill;


public sealed class CalendarSkill
{
    public static class Parameters
    {
        public const string START = "start";
        public const string END = "end";
        public const string LOCATION = "location";
        public const string CONTENT = "content";
        public const string ATTENDEES = "attendees";
        public const string MAX_RESULTS = "maxResults";
        public const string SKIP = "skip";
    }

    private readonly ICalendarConnector _connector;
    private readonly ILogger _logger;

    public CalendarSkill()
    {
    }

    public CalendarSkill(ICalendarConnector connector, ILogger logger, ILoggerFactory? loggerFactory = null)
    {
        Verify.NotNull(connector, nameof(connector));

        _connector = connector;
        _logger = loggerFactory is not null ? loggerFactory.CreateLogger(typeof(CalendarSkill)) : NullLogger.Instance;
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    [SKFunction, Description("Add an event to my calendar.")]
    public async Task AddEventAsync(
        [Description("Event subject"), SKName("input")] string subject,
        [Description("Event start date/time as DateTimeOffset")] DateTimeOffset start,
        [Description("Event end date/time as DateTimeOffset")] DateTimeOffset end,
        [Description("Event location (optional)")] string? location = null,
        [Description("Event content/body (optional)")] string? content = null,
        [Description("Event attendees, separated by ',' or ';'.")] string? attendees = null)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException($"{nameof(subject)} variable was null or whitespace", nameof(subject));
        }

        CalendarEvent calendarEvent = new()
        {
            Subject = subject,
            Start = start,
            End = end,
            Location = location,
            Content = content,
            Attendees = attendees?.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries) ?? Enumerable.Empty<string>(),
        };

        _logger.LogTrace("Adding calendar event '{0}'", calendarEvent.Subject);
        await _connector!.AddEventAsync(calendarEvent).ConfigureAwait(false);
    }

    [SKFunction, Description("Get calendar events.")]
    public async Task<string> GetCalendarEventsAsync(
        [Description("Optional limit of the number of events to retrieve.")] int? maxResults = 10,
        [Description("Optional number of events to skip before retrieving results.")] int? skip = 0,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting calendar events with query options top: '{0}', skip:'{1}'.", maxResults, skip);

        const string SelectString = "start,subject,organizer,location";

        var events = await _connector!.GetEventsAsync(
            top: maxResults,
            skip: skip,
            select: SelectString,
            cancellationToken
        ).ConfigureAwait(false);

        return JsonSerializer.Serialize(value: events, options: Options);
    }
}
