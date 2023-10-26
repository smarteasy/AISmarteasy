using AISmarteasy.Core.PluginFunction;
using Microsoft.Graph;

namespace AISmarteasy.Core.Connecting.MicrosoftGraph;

public class OutlookMailConnector : IEmailConnector
{
    private readonly GraphServiceClient _graphServiceClient;

    public OutlookMailConnector(GraphServiceClient graphServiceClient)
    {
        _graphServiceClient = graphServiceClient;
    }

    public async Task<string> GetMyEmailAddressAsync(CancellationToken cancellationToken = default)
        => (await _graphServiceClient.Me.Request().GetAsync(cancellationToken).ConfigureAwait(false)).UserPrincipalName;

    public async Task SendEmailAsync(string subject, string content, string[] recipients, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhitespace(subject, nameof(subject));
        Verify.NotNullOrWhitespace(content, nameof(content));
        Verify.NotNull(recipients, nameof(recipients));

        Message message = new()
        {
            Subject = subject,
            Body = new ItemBody { ContentType = BodyType.Text, Content = content },
            ToRecipients = recipients.Select(recipientAddress => new Recipient
            {
                EmailAddress = new()
                {
                    Address = recipientAddress
                }
            })
        };

        await _graphServiceClient.Me.SendMail(message).Request().PostAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<EmailMessage>> GetMessagesAsync(
        int? top, int? skip, string? select, CancellationToken cancellationToken = default)
    {
        IUserMessagesCollectionRequest query = _graphServiceClient.Me.Messages.Request();

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

        IUserMessagesCollectionPage result = await query.GetAsync(cancellationToken).ConfigureAwait(false);

        IEnumerable<EmailMessage> messages = result.Select(message => message.ToEmailMessage());

        return messages;
    }
}
