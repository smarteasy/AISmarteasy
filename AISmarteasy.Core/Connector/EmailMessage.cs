namespace AISmarteasy.Core.Connector;

public class EmailMessage
{
    public EmailAddress? From { get; set; }

    public IEnumerable<EmailAddress>? Recipients { get; set; }

    public IEnumerable<EmailAddress>? CcRecipients { get; set; }


    public IEnumerable<EmailAddress>? BccRecipients { get; set; }

    public string? Subject { get; set; }

    public string? Body { get; set; }


    public string? BodyPreview { get; set; }

    public bool? IsRead { get; set; }

    public DateTimeOffset? ReceivedDateTime { get; set; }

    public DateTimeOffset? SentDateTime { get; set; }
}
