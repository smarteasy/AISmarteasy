using Microsoft.Graph;

namespace AISmarteasy.Core.Connector.MicrosoftGraph;

public class OrganizationHierarchyConnector : IOrganizationHierarchyConnector
{
    private readonly GraphServiceClient _graphServiceClient;

    public OrganizationHierarchyConnector(GraphServiceClient graphServiceClient)
    {
        this._graphServiceClient = graphServiceClient;
    }

    public async Task<string> GetManagerEmailAsync(CancellationToken cancellationToken = default) =>
        ((User)await this._graphServiceClient.Me
            .Manager
            .Request().GetAsync(cancellationToken).ConfigureAwait(false)).UserPrincipalName;

    public async Task<string> GetManagerNameAsync(CancellationToken cancellationToken = default) =>
        ((User)await this._graphServiceClient.Me
            .Manager
            .Request().GetAsync(cancellationToken).ConfigureAwait(false)).DisplayName;

    public async Task<IEnumerable<string>> GetDirectReportsEmailAsync(CancellationToken cancellationToken = default)
    {
        IUserDirectReportsCollectionWithReferencesPage directsPage = await this._graphServiceClient.Me
            .DirectReports
            .Request().GetAsync(cancellationToken).ConfigureAwait(false);

        List<User> directs = directsPage.Cast<User>().ToList();

        while (directs.Count != 0 && directsPage.NextPageRequest != null)
        {
            directsPage = await directsPage.NextPageRequest.GetAsync(cancellationToken).ConfigureAwait(false);
            directs.AddRange(directsPage.Cast<User>());
        }

        return directs.Select(d => d.UserPrincipalName);
    }
}
