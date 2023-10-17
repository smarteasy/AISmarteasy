namespace AISmarteasy.Core.Connecting;

public interface IEmailConnector
{
    Task<string> GetMyEmailAddressAsync(CancellationToken cancellationToken = default);

    Task SendEmailAsync(string subject, string content, string[] recipients, CancellationToken cancellationToken = default);

    Task<IEnumerable<EmailMessage>> GetMessagesAsync(int? top, int? skip, string? @select, CancellationToken cancellationToken = default);
}
