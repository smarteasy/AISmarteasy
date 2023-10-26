using System.Net;
using AISmarteasy.Core.PluginFunction;
using AISmarteasy.Core.Handling;
using Microsoft.Graph;

namespace AISmarteasy.Core.Connecting.MicrosoftGraph;

public class OneDriveConnector : ICloudDriveConnector
{
    private readonly GraphServiceClient _graphServiceClient;

    public OneDriveConnector(GraphServiceClient graphServiceClient)
    {
        this._graphServiceClient = graphServiceClient;
    }

    public async Task<Stream> GetFileContentStreamAsync(string filePath, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhitespace(filePath, nameof(filePath));

        return await this._graphServiceClient.Me
            .Drive.Root
            .ItemWithPath(filePath).Content
            .Request().GetAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhitespace(filePath, nameof(filePath));

        try
        {
            await this._graphServiceClient.Me
                .Drive.Root
                .ItemWithPath(filePath).Request().GetAsync(cancellationToken).ConfigureAwait(false);

            return true;
        }
        catch (ServiceException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            throw new HttpOperationException(ex.StatusCode, responseContent: null, ex.Message, ex);
        }
    }

    public async Task UploadSmallFileAsync(string filePath, string destinationPath, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhitespace(filePath, nameof(filePath));
        Verify.NotNullOrWhitespace(destinationPath, nameof(destinationPath));

        filePath = Environment.ExpandEnvironmentVariables(filePath);

        long fileSize = new FileInfo(filePath).Length;
        if (fileSize > 4 * 1024 * 1024)
        {
            throw new IOException("File is too large to upload - function currently only supports files up to 4MB.");
        }

        FileStream fileContentStream = new(filePath, FileMode.Open, FileAccess.Read);

        GraphResponse<DriveItem>? response = null;

        try
        {
            response = await _graphServiceClient.Me
                .Drive.Root
                .ItemWithPath(destinationPath).Content
                .Request().PutResponseAsync<DriveItem>(fileContentStream, cancellationToken, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);

            response.ToHttpResponseMessage().EnsureSuccessStatusCode();
        }
        catch (ServiceException ex)
        {
            throw new HttpOperationException(ex.StatusCode, responseContent: null, ex.Message, ex);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpOperationException(response?.StatusCode, responseContent: null, ex.Message, ex);
        }
    }

    /// <inheritdoc/>
    public async Task<string> CreateShareLinkAsync(string filePath, string type = "view", string scope = "anonymous",
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhitespace(filePath, nameof(filePath));
        Verify.NotNullOrWhitespace(type, nameof(type));
        Verify.NotNullOrWhitespace(scope, nameof(scope));

        GraphResponse<Permission>? response = null;

        try
        {
            response = await this._graphServiceClient.Me
               .Drive.Root
               .ItemWithPath(filePath)
               .CreateLink(type, scope)
               .Request().PostResponseAsync(cancellationToken).ConfigureAwait(false);

            response.ToHttpResponseMessage().EnsureSuccessStatusCode();
        }
        catch (ServiceException ex)
        {
            throw new HttpOperationException(ex.StatusCode, responseContent: null, ex.Message, ex);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpOperationException(response?.StatusCode, responseContent: null, ex.Message, ex);
        }

        string? result = (await response.GetResponseObjectAsync().ConfigureAwait(false)).Link?.WebUrl;
        if (string.IsNullOrWhiteSpace(result))
        {
            throw new SKException("Shareable file link was null or whitespace.");
        }

        return result;
    }
}
