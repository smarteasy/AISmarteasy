using System.ComponentModel;
using AISmarteasy.Core.Connecting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISmarteasy.Core.PluginFunction.NativeSkill;

public sealed class CloudDriveSkill
{
    public static class Parameters
    {
        public const string DESTINATION_PATH = "destinationPath";
    }

    private readonly ICloudDriveConnector _connector;
    private readonly ILogger _logger;

    public CloudDriveSkill()
    {
    }

    public CloudDriveSkill(ICloudDriveConnector connector, ILoggerFactory? loggerFactory = null)
    {
        Verify.NotNull(connector, nameof(connector));

        this._connector = connector;
        this._logger = loggerFactory is not null ? loggerFactory.CreateLogger(typeof(CloudDriveSkill)) : NullLogger.Instance;
    }

    [SKFunction, Description("Get the contents of a file in a cloud drive.")]
    public async Task<string> GetFileContentAsync(
        [Description("Path to file")] string filePath,
        CancellationToken cancellationToken = default)
    {
        this._logger.LogDebug("Getting file content for '{0}'", filePath);
        Stream fileContentStream = await this._connector.GetFileContentStreamAsync(filePath, cancellationToken).ConfigureAwait(false);

        using StreamReader sr = new(fileContentStream);
        string content = await sr.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        return content;
    }

    [SKFunction, Description("Upload a small file to OneDrive (less than 4MB).")]
    public async Task UploadFileAsync(
        [Description("Path to file")] string filePath,
        [Description("Remote path to store the file")] string destinationPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            throw new ArgumentException("Variable was null or whitespace", nameof(destinationPath));
        }

        _logger.LogDebug("Uploading file '{0}'", filePath);

        await _connector.UploadSmallFileAsync(filePath, destinationPath, cancellationToken).ConfigureAwait(false);
    }

    [SKFunction, Description("Create a sharable link to a file stored in a cloud drive.")]
    public async Task<string> CreateLinkAsync(
        [Description("Path to file")] string filePath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating link for '{0}'", filePath);
        const string Type = "view"; 
        const string Scope = "anonymous"; 

        return await _connector.CreateShareLinkAsync(filePath, Type, Scope, cancellationToken).ConfigureAwait(false);
    }
}
