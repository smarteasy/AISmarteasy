using Microsoft.Graph;
using Microsoft.Graph.Extensions;

namespace AISmarteasy.Core.Connecting.MicrosoftGraph;

internal static class MicrosoftGraphModelExtensions
{
    public static EmailMessage ToEmailMessage(this Message graphMessage)
        => new()
        {
            BccRecipients = graphMessage.BccRecipients?.Select(r => r.EmailAddress.ToEmailAddress()),
            Body = graphMessage.Body?.Content,
            BodyPreview = graphMessage.BodyPreview.Replace("\u200C", ""),
            CcRecipients = graphMessage.CcRecipients?.Select(r => r.EmailAddress.ToEmailAddress()),
            From = graphMessage.From?.EmailAddress?.ToEmailAddress(),
            IsRead = graphMessage.IsRead,
            ReceivedDateTime = graphMessage.ReceivedDateTime,
            Recipients = graphMessage.ToRecipients?.Select(r => r.EmailAddress.ToEmailAddress()),
            SentDateTime = graphMessage.SentDateTime,
            Subject = graphMessage.Subject
        };

    public static EmailAddress ToEmailAddress(this Microsoft.Graph.EmailAddress graphEmailAddress)
        => new()
        {
            Address = graphEmailAddress.Address,
            Name = graphEmailAddress.Name
        };

    public static Event ToGraphEvent(this CalendarEvent calendarEvent)
        => new()
        {
            Subject = calendarEvent.Subject,
            Body = new ItemBody { Content = calendarEvent.Content, ContentType = BodyType.Html },
            Start = calendarEvent.Start.HasValue
                ? DateTimeTimeZone.FromDateTimeOffset(calendarEvent.Start.Value)
                : DateTimeTimeZone.FromDateTime(DateTime.Now),
            End = calendarEvent.End.HasValue
                ? DateTimeTimeZone.FromDateTimeOffset(calendarEvent.End.Value)
                : DateTimeTimeZone.FromDateTime(DateTime.Now + TimeSpan.FromHours(1)),
            Location = new Location { DisplayName = calendarEvent.Location },
            Attendees = calendarEvent.Attendees?.Select(a => new Attendee { EmailAddress = new Microsoft.Graph.EmailAddress { Address = a } })
        };

    public static CalendarEvent ToCalendarEvent(this Event msGraphEvent)
        => new()
        {
            Subject = msGraphEvent.Subject,
            Content = msGraphEvent.Body?.Content,
            Start = msGraphEvent.Start?.ToDateTimeOffset(),
            End = msGraphEvent.End?.ToDateTimeOffset(),
            Location = msGraphEvent.Location?.DisplayName,
            Attendees = msGraphEvent.Attendees?.Select(a => a.EmailAddress.Address)
        };
}
