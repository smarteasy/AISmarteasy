using System.ComponentModel;
using System.Text.Json;
using AISmarteasy.Core.Connecting;

namespace AISmarteasy.Core.PluginFunction.NativeSkill;

public sealed class OrganizationHierarchySkill
{
    private readonly IOrganizationHierarchyConnector _connector;

    public OrganizationHierarchySkill()
    {
    }

    public OrganizationHierarchySkill(IOrganizationHierarchyConnector connector)
    {
        Verify.NotNull(connector, nameof(connector));

        this._connector = connector;
    }

    [SKFunction, Description("Get my direct report's email addresses.")]
    public async Task<string> GetMyDirectReportsEmailAsync(CancellationToken cancellationToken = default)
        => JsonSerializer.Serialize(await this._connector.GetDirectReportsEmailAsync(cancellationToken).ConfigureAwait(false));

    [SKFunction, Description("Get my manager's email address.")]
    public async Task<string> GetMyManagerEmailAsync(CancellationToken cancellationToken = default)
        => await this._connector.GetManagerEmailAsync(cancellationToken).ConfigureAwait(false);

    [SKFunction, Description("Get my manager's name.")]
    public async Task<string> GetMyManagerNameAsync(CancellationToken cancellationToken = default)
        => await this._connector.GetManagerNameAsync(cancellationToken).ConfigureAwait(false);
}
