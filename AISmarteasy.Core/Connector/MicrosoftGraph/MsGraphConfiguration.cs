using System.Diagnostics.CodeAnalysis;

namespace AISmarteasy.Core.Connector.MicrosoftGraph;

public class MsGraphConfiguration
{
    public string ClientId { get; }

    public string TenantId { get; }

    public IEnumerable<string> Scopes { get; set; } = Enumerable.Empty<string>();

    public Uri RedirectUri { get; }

    public MsGraphConfiguration(
        [NotNull] string clientId,
        [NotNull] string tenantId,
        [NotNull] Uri redirectUri)
    {
        ClientId = clientId;
        TenantId = tenantId;
        RedirectUri = redirectUri;
    }
}
