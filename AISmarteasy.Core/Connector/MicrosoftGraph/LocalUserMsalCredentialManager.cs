using System.Collections.Concurrent;
using AISmarteasy.Core.Function;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace AISmarteasy.Core.Connector.MicrosoftGraph;

public sealed class LocalUserMsalCredentialManager
{
    private readonly ConcurrentDictionary<string, IPublicClientApplication> _publicClientApplications;

    private readonly StorageCreationProperties _storageProperties;

    private readonly MsalCacheHelper _cacheHelper;

    private LocalUserMsalCredentialManager(StorageCreationProperties storage, MsalCacheHelper cacheHelper)
    {
        _publicClientApplications = new ConcurrentDictionary<string, IPublicClientApplication>(StringComparer.OrdinalIgnoreCase);
        _storageProperties = storage;
        _cacheHelper = cacheHelper;
        _cacheHelper.VerifyPersistence();
    }

    public static async Task<LocalUserMsalCredentialManager> CreateAsync()
    {
        const string CacheSchemaName = "com.microsoft.semantickernel.tokencache";

        var storage = new StorageCreationPropertiesBuilder("sk.msal.cache", MsalCacheHelper.UserRootDirectory)
            .WithMacKeyChain(
                serviceName: $"{CacheSchemaName}.service",
                accountName: $"{CacheSchemaName}.account")
            .WithLinuxKeyring(
                schemaName: CacheSchemaName,
                collection: MsalCacheHelper.LinuxKeyRingDefaultCollection,
                secretLabel: "MSAL token cache for Semantic Kernel plugins.",
                attribute1: new KeyValuePair<string, string>("Version", "1"),
                attribute2: new KeyValuePair<string, string>("Product", "SemanticKernel"))
            .Build();

        var cacheHelper = await MsalCacheHelper.CreateAsync(storage).ConfigureAwait(false);

        return new LocalUserMsalCredentialManager(storage, cacheHelper);
    }

    public async Task<string> GetTokenAsync(string clientId, string tenantId, string[] scopes, Uri redirectUri)
    {
        Verify.NotNullOrWhitespace(clientId, nameof(clientId));
        Verify.NotNullOrWhitespace(tenantId, nameof(tenantId));
        Verify.NotNull(redirectUri, nameof(redirectUri));
        Verify.NotNull(scopes, nameof(scopes));

        IPublicClientApplication app = this._publicClientApplications.GetOrAdd(
            key: PublicClientApplicationsKey(clientId, tenantId),
            valueFactory: _ =>
            {
                IPublicClientApplication newPublicApp = PublicClientApplicationBuilder.Create(clientId)
                    .WithRedirectUri(redirectUri.ToString())
                    .WithTenantId(tenantId)
                    .Build();
                this._cacheHelper.RegisterCache(newPublicApp.UserTokenCache);
                return newPublicApp;
            });

        IEnumerable<IAccount> accounts = await app.GetAccountsAsync().ConfigureAwait(false);

        AuthenticationResult result;
        try
        {
            result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                .ExecuteAsync().ConfigureAwait(false);
        }
        catch (MsalUiRequiredException)
        {
            result = await app.AcquireTokenInteractive(scopes)
                .ExecuteAsync().ConfigureAwait(false);
        }

        return result.AccessToken;
    }

    private static string PublicClientApplicationsKey(string clientId, string tenantId) => $"{clientId}_{tenantId}";
}
